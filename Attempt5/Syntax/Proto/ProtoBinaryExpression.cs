using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public enum BinaryOperator {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    public class ProtoBinaryExpression : IProtoSyntax {
        public BinaryOperator Operator { get; }

        public IProtoSyntax LeftTarget { get; }

        public IProtoSyntax RightTarget { get; }

        public ProtoBinaryExpression(BinaryOperator op, IProtoSyntax left, IProtoSyntax right) {
            this.Operator = op;
            this.LeftTarget = left;
            this.RightTarget = right;
        }

        public void Accept(IProtoSyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}