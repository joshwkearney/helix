using System.Collections.Generic;
using System.Linq;

namespace Attempt9 {
    public class IfStatement : IStatementSyntax {
        public IExpressionSyntax Condition { get; }

        public IReadOnlyList<IStatementSyntax> AffirmativeBlock { get; }

        public IReadOnlyList<IStatementSyntax> NegativeBlock { get; }

        public IfStatement(IExpressionSyntax cond, IEnumerable<IStatementSyntax> affirm, IEnumerable<IStatementSyntax> neg) {
            this.Condition = cond;
            this.AffirmativeBlock = affirm.ToArray();
            this.NegativeBlock = neg.ToArray();
        }

        public void Accept(IStatementVisitor visitor) => visitor.Visit(this);
    }
}