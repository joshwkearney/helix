namespace Attempt9 {
    public enum SyntaxBinaryOperator {
        Add, Subtract, Multiply, Divide
    }

    public class BinaryExpressionSyntax : IExpressionSyntax {
        public SyntaxBinaryOperator Operator { get; }

        public IExpressionSyntax RightOperand { get; }

        public IExpressionSyntax LeftOperand { get; }

        public ITrophyType ReturnType { get; }

        public BinaryExpressionSyntax(SyntaxBinaryOperator op, IExpressionSyntax left, IExpressionSyntax right, ITrophyType type) {
            this.Operator = op;
            this.RightOperand = right;
            this.LeftOperand = left;
            this.ReturnType = type;
        }

        public void Accept(IExpressionVisitor visitor) => visitor.Visit(this);
    }
}