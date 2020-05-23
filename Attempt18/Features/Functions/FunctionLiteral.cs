using Attempt18.Evaluation;
using Attempt18.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt18.Features.Functions {
    public class FunctionLiteral : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public IdentifierPath FunctionPath { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.CapturedVariables = new IdentifierPath[0];
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            throw new InvalidOperationException();
        }

        public void DeclareTypes(TypeChache cache) {
            throw new InvalidOperationException();
        }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            return memory[this.FunctionPath];
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) { }

        public void ResolveNames(NameCache<NameTarget> names) {
            throw new InvalidOperationException();
        }

        public void ResolveScope(IdentifierPath containingScope) {
            throw new InvalidOperationException();
        }

        public ISyntax ResolveTypes(TypeChache types) {
            this.ReturnType = new FunctionType(this.FunctionPath);

            return this;
        }
    }
}
