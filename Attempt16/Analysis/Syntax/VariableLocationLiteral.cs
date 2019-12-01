using System.Collections.Generic;
using Attempt16.Analysis;
using Attempt16.Types;

namespace Attempt16.Syntax {
    public class VariableLocationLiteral : ISyntax {
        public ILanguageType ReturnType { get; set; }

        public string VariableName { get; set; }

        public VariableSource Source { get; set; }

        public HashSet<IdentifierPath> ReturnCapturedVariables { get; set; } = new HashSet<IdentifierPath>();

        public T Accept<T>(ISyntaxVisitor<T> visitor) {
            return visitor.VisitVariableLocationLiteral(this);
        }
    }
}