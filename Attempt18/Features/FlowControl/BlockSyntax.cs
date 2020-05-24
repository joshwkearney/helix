using System;
using System.Collections.Generic;
using System.Linq;
using Attempt19.Evaluation;
using Attempt19.Types;

namespace Attempt19.Features.FlowControl {
    public class BlockSyntax : ISyntax {
        private static int id = 0;

        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public ISyntax[] Statements { get; set; }

        public IdentifierPath BlockPath { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            // Analyze statements
            foreach (var stat in this.Statements) {
                stat.AnalyzeFlow(types, flow);
            }

            if (this.Statements.Any()) {
                var retValue = this.Statements[this.Statements.Length - 1];

                // Make sure that the return value doesn't capture any variable inside the scope
                foreach (var cap in retValue.CapturedVariables) {
                    if (cap.StartsWith(this.BlockPath)) {
                        throw new Exception();
                    }
                }

                this.CapturedVariables = retValue.CapturedVariables;
            }
            else {
                this.CapturedVariables = new IdentifierPath[0];
            }
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            // Declare statement globals
            foreach (var stat in this.Statements) {
                stat.DeclareNames(names);
            }
        }

        public void DeclareTypes(TypeChache  cache) {
            foreach (var stat in this.Statements) {
                stat.DeclareTypes(cache);
            }
        }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            var values = this.Statements.Select(x => x.Evaluate(memory)).ToArray();

            if (this.Statements.Any()) {
                return values.Last();
            }
            else {
                return new AtomicEvaluateResult(0);
            }
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            foreach (var stat in this.Statements) {
                stat.PreEvaluate(memory);
            }
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            names.PushLocalFrame();

            // Resolve statement names
            foreach (var stat in this.Statements) {
                stat.ResolveNames(names);
            }

            names.PopLocalFrame();
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;
            this.BlockPath = this.Scope.Append("_block" + id++);

            // Resolve the statements with this scope
            foreach (var stat in this.Statements) {
                stat.ResolveScope(this.BlockPath);
            }
        }

        public ISyntax ResolveTypes(TypeChache  types) {
            // Type check statements
            this.Statements = this.Statements.Select(x => x.ResolveTypes(types)).ToArray();

            if (this.Statements.Any()) {
                this.ReturnType = this.Statements.Last().ReturnType;
            }
            else {
                this.ReturnType = VoidType.Instance;
            }

            return this;
        }
    }
}