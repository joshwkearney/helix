using Attempt16.Analysis;
using Attempt16.Types;
using System.Collections.Generic;

namespace Attempt16.Syntax {
    public class VariableLiteral : ISyntax {
        public string VariableName { get; set; }

        public ILanguageType ReturnType { get; set; }

        public VariableSource Source { get; set; }

        public HashSet<IdentifierPath> ReturnCapturedVariables { get; set; } = new HashSet<IdentifierPath>();

        public T Accept<T>(ISyntaxVisitor<T> visitor) {
            return visitor.VisitVariableLiteral(this);
        }
    }
}
