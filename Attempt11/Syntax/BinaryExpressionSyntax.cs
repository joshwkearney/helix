namespace Attempt10 {
    public enum BinaryOperator {
        Add, Subtract, Multiply,
        StrictDivide, RealDivide,
        LogicalAnd, BitwiseAnd,
        LogicalOr, BitwiseOr,
        Xor
    }

    public class BinaryExpressionSyntax : ISyntaxTree {
        public ISyntaxTree Left { get; }

        public ISyntaxTree Right { get; }

        public BinaryOperator Operator { get; }

        public ITrophyType ExpressionType { get; }

        public Scope Scope { get; }

        public BinaryExpressionSyntax(ISyntaxTree left, ISyntaxTree right, BinaryOperator op, ITrophyType returnType, Scope scope) {
            this.Left = left;
            this.Right = right;
            this.Operator = op;
            this.ExpressionType = returnType;
            this.Scope = scope;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}