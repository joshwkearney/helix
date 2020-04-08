using System;
using System.Collections.Generic;
using Attempt18.Types;

namespace Attempt18.Features.FlowControl {
    public class WhileSyntax : ISyntax {
        private static int id = 0;

        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public ISyntax Condition { get; set; }

        public ISyntax Body { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.CapturedVariables = new IdentifierPath[0];
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            this.Condition.DeclareNames(names);
            this.Body.DeclareNames(names);
        }

        public void DeclareTypes(TypeChache  cache) {
            this.Condition.DeclareTypes(cache);
            this.Body.DeclareTypes(cache);
        }

        public object Evaluate(Dictionary<IdentifierPath, object> memory) {
            var cond = (bool)this.Condition.Evaluate(memory);

            while (true) {
                if (!cond) {
                    break;
                }

                this.Body.Evaluate(memory);
                cond = (bool)this.Condition.Evaluate(memory);
            }

            return 0;
        }

        public void PreEvaluate(Dictionary<IdentifierPath, object> memory) {
            this.Condition.PreEvaluate(memory);
            this.Body.PreEvaluate(memory);
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            this.Condition.ResolveNames(names);
            this.Body.ResolveNames(names);
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;

            var whileScope = this.Scope.Append("_while" + id++);

            this.Condition.ResolveScope(this.Scope);
            this.Body.ResolveScope(whileScope);
        }

        public ISyntax ResolveTypes(TypeChache  types) {
            this.Condition = this.Condition.ResolveTypes(types);
            this.Body = this.Body.ResolveTypes(types);

            this.ReturnType = VoidType.Instance;

            return this;
        }
    }
}
