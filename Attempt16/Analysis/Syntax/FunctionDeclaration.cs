using Attempt16.Analysis;
using Attempt16.Types;
using System.Collections.Generic;

namespace Attempt16.Syntax {
    public class FunctionDeclaration : IDeclaration {
        public string Name { get; set; }

        public IReadOnlyList<FunctionParameter> Parameters { get; set; }

        public IdentifierPath ReturnType { get; set; }

        public ISyntax Body { get; set; }

        public SingularFunctionType FunctionType { get; set; }

        public T Accept<T>(IDeclarationVisitor<T> visitor) {
            return visitor.VisitFunctionDeclaration(this);
        }
    }
}