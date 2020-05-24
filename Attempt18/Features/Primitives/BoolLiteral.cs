using System;
using System.Collections.Generic;
using Attempt19.Evaluation;
using Attempt19.Types;

namespace Attempt19.Features.Primitives {
    public class BoolLiteral : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public bool Value { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.CapturedVariables = new IdentifierPath[0];
        }

        public void DeclareNames(NameCache<NameTarget> names) { }

        public void DeclareTypes(TypeChache cache) { }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            return new AtomicEvaluateResult(this.Value);
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) { }

        public void ResolveNames(NameCache<NameTarget> names) { }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;
        }

        public ISyntax ResolveTypes(TypeChache  types) {
            this.ReturnType = BoolType.Instance;

            return this;
        }
    }
}
