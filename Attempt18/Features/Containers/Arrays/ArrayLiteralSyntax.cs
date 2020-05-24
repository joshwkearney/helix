using System;
using System.Collections.Generic;
using System.Linq;
using Attempt19.Evaluation;
using Attempt19.Types;

namespace Attempt19.Features.Containers.Arrays {
    public class ArrayLiteralSyntax : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public ISyntax[] Values { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            foreach (var val in this.Values) {
                val.AnalyzeFlow(types, flow);
            }

            this.CapturedVariables = this.Values
                .SelectMany(x => x.CapturedVariables)
                .Distinct()
                .ToArray();
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            foreach (var val in this.Values) {
                val.DeclareNames(names);
            }
        }

        public void DeclareTypes(TypeChache  cache) {
            foreach (var val in this.Values) {
                val.DeclareTypes(cache);
            }
        }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            var array = this.Values
                .Select(x => x.Evaluate(memory))
                .ToArray();

            return new AtomicEvaluateResult(array);
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            foreach (var value in this.Values) {
                value.PreEvaluate(memory);
            }
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            foreach (var val in this.Values) {
                val.ResolveNames(names);
            }
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;

            foreach (var val in this.Values) {
                val.ResolveScope(containingScope);
            }
        }

        public ISyntax ResolveTypes(TypeChache  types) {
            // Make sure there is at least one value
            if (!this.Values.Any()) {
                throw new Exception();
            }

            // Type check the values
            this.Values = this.Values.Select(x => x.ResolveTypes(types)).ToArray();

            // Make sure all values match
            var type = this.Values.First().ReturnType;
            foreach (var val in this.Values) {
                if (val.ReturnType != type) {
                    throw new Exception();
                }
            }

            this.ReturnType = new ArrayType(type);

            return this;
        }
    }
}
