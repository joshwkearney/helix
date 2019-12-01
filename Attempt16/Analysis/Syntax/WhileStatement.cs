using System.Collections.Generic;
using Attempt16.Analysis;
using Attempt16.Types;

namespace Attempt16.Syntax {
    public class WhileStatement : ISyntax {
        public ILanguageType ReturnType { get; set; }

        public HashSet<IdentifierPath> ReturnCapturedVariables { get; set; } = new HashSet<IdentifierPath>();

        public ISyntax Condition { get; set; }

        public ISyntax Body { get; set; }

        public T Accept<T>(ISyntaxVisitor<T> visitor) {
            return visitor.VisitWhileStatement(this);
        }
    }
}