using System.Collections.Generic;
using Attempt16.Analysis;
using Attempt16.Types;

namespace Attempt16.Syntax {
    public class IfSyntax : ISyntax {
        public ILanguageType ReturnType { get; set; }

        public ISyntax Condition { get; set; }

        public ISyntax Affirmative { get; set; }

        public ISyntax Negative { get; set; }

        public HashSet<IdentifierPath> ReturnCapturedVariables { get; set; } = new HashSet<IdentifierPath>();

        public T Accept<T>(ISyntaxVisitor<T> visitor) {
            return visitor.VisitIf(this);
        }
    }
}
