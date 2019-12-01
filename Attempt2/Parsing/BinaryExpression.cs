using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt2.Parsing {
    public enum BinaryOperator {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    public class BinaryExpression : IAST {
        public BinaryOperator Operator { get; }

        public IAST LeftTarget { get; }

        public IAST RightTarget { get; }

        public BinaryExpression(BinaryOperator op, IAST left, IAST right) {
            this.Operator = op;
            this.LeftTarget = left;
            this.RightTarget = right;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.VisitBinaryExpression(this);
        }
    }
}