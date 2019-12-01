using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12 {
    public enum PrimitiveOperation {
        Int64Negate, Int64Not,
        Int64Add, Int64Subtract, Int64Multiply, Int64RealDivide, Int64StrictDivide,
        Int64And, Int64Or, Int64Xor,
        Int64GreaterThan, Int64LessThan,

        Real64Negate,
        Real64Add, Real64Subtract, Real64Multiply, Real64Divide,
        Real64GreaterThan, Real64LessThan,

        BooleanNot,
        BooleanAnd, BooleanOr, BooleanXor
    }

    public class PrimitiveOperationSyntax : ISyntaxTree {
        public ITrophyType ExpressionType { get; }

        public Scope Scope { get; }

        public IReadOnlyList<ISyntaxTree> Operands { get; }

        public PrimitiveOperation Operation { get; }

        public bool IsConstant => true;

        public PrimitiveOperationSyntax(ITrophyType type, PrimitiveOperation operation, Scope scope, params ISyntaxTree[] operands) {
            this.ExpressionType = type;
            this.Operation = operation;
            this.Operands = operands;
            this.Scope = scope;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}