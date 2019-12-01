using Attempt16.Analysis;
using Attempt16.Types;
using System.Collections.Generic;

namespace Attempt16.Syntax {
    public enum DeclarationOperation {
        Store, Equate
    }

    public class VariableStatement : ISyntax {
        public string VariableName { get; set; }

        public ISyntax Value { get; set; }

        public ILanguageType ReturnType { get; set; }

        public DeclarationOperation Operation { get; set; }

        public HashSet<IdentifierPath> ReturnCapturedVariables { get; set; } = new HashSet<IdentifierPath>();

        public int VarCount { get; set; }

        public T Accept<T>(ISyntaxVisitor<T> visitor) {
            return visitor.VisitVariableInitialization(this);
        }
    }
}
