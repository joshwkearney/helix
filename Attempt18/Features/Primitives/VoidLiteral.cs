using System;
using System.Collections.Generic;
using Attempt18.Evaluation;
using Attempt18.Types;

namespace Attempt18.Features.Primitives {
    public class VoidLiteral : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.CapturedVariables = new IdentifierPath[0];
        }

        public void DeclareNames(NameCache<NameTarget> names) { }

        public void DeclareTypes(TypeChache  cache) { }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            return new AtomicEvaluateResult(0);
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) { }

        public void ResolveNames(NameCache<NameTarget> names) { }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;
        }

        public ISyntax ResolveTypes(TypeChache  types) {
            this.ReturnType = VoidType.Instance;

            return this;
        }
    }
}
