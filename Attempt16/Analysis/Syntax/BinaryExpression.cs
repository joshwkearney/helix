using System.Collections.Generic;
using Attempt16.Analysis;
using Attempt16.Types;

namespace Attempt16.Syntax {
    public class BinaryExpression : ISyntax {
        public ILanguageType ReturnType { get; set; }

        public HashSet<IdentifierPath> ReturnCapturedVariables { get; set; } = new HashSet<IdentifierPath>();

        public ISyntax Left { get; set; }

        public ISyntax Right { get; set; }

        public BinaryOperator Operation { get; set; }

        public T Accept<T>(ISyntaxVisitor<T> visitor) {
            return visitor.VisitBinaryExpression(this);
        }
    }
}