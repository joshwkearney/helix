using System.Collections.Generic;
using Attempt16.Analysis;
using Attempt16.Types;

namespace Attempt16.Syntax {
    public class BlockSyntax : ISyntax {
        public IReadOnlyList<ISyntax> Statements { get; set; }

        public ILanguageType ReturnType { get; set; }

        public HashSet<IdentifierPath> ReturnCapturedVariables { get; set; } = new HashSet<IdentifierPath>();

        public T Accept<T>(ISyntaxVisitor<T> visitor) {
            return visitor.VisitBlock(this);
        }
    }
}
