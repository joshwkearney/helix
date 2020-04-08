using System;
using System.Collections.Generic;
using Attempt18.Types;

namespace Attempt18.Features.Functions {
    public class FunctionLiteral : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            throw new NotImplementedException();
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            throw new InvalidOperationException();
        }

        public void DeclareTypes(TypeChache cache) {
            throw new InvalidOperationException();
        }

        public object Evaluate(Dictionary<IdentifierPath, object> memory) {
            throw new NotImplementedException();
        }

        public void PreEvaluate(Dictionary<IdentifierPath, object> memory) {
            throw new NotImplementedException();
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            throw new InvalidOperationException();
        }

        public void ResolveScope(IdentifierPath containingScope) {
            throw new InvalidOperationException();
        }

        public ISyntax ResolveTypes(TypeChache types) {
            throw new InvalidOperationException();
        }
    }
}
