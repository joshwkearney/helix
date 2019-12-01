using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt2.Parsing {
    public enum UnaryOperator {
        Posate,
        Negate
    }

    public class UnaryExpression : IAST {
        public UnaryOperator Operator { get; }

        public IAST Target { get; }

        public UnaryExpression(UnaryOperator op, IAST target) {
            this.Operator = op;
            this.Target = target;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.VisitUnaryExpression(this);
        }
    }
}