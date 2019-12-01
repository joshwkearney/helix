using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt2.Parsing {
    public class IfExpression : IAST {
        public IAST Condition { get; }

        public IAST AffirmativeExpression { get; }

        public IAST NegativeExpression { get; }

        public IfExpression(IAST cond, IAST ifTrue, IAST ifFalse) {
            this.Condition = cond;
            this.AffirmativeExpression = ifTrue;
            this.NegativeExpression = ifFalse;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.VisitIfExpression(this);
        }
    }
}