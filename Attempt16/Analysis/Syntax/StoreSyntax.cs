using System.Collections.Generic;
using Attempt16.Analysis;
using Attempt16.Types;

namespace Attempt16.Syntax {
    public class StoreSyntax : ISyntax {
        public ILanguageType ReturnType { get; set; }

        public ISyntax Target { get; set; }

        public ISyntax Value { get; set; }

        public HashSet<IdentifierPath> ReturnCapturedVariables { get; set; } = new HashSet<IdentifierPath>();

        public T Accept<T>(ISyntaxVisitor<T> visitor) {
            return visitor.VisitStore(this);
        }
    }
}