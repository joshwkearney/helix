namespace Attempt9 {
    public enum SyntaxUnaryOperator {
        Negation, Unbox
    }

    public class UnaryExpressionSyntax : IExpressionSyntax {
        public SyntaxUnaryOperator Operator { get; }

        public IExpressionSyntax Operand { get; }

        public ITrophyType ReturnType { get; }

        public UnaryExpressionSyntax(SyntaxUnaryOperator op, IExpressionSyntax operand, ITrophyType type) {
            this.Operand = operand;
            this.Operator = op;
            this.ReturnType = type;
        }

        public void Accept(IExpressionVisitor visitor) => visitor.Visit(this);
    }
}