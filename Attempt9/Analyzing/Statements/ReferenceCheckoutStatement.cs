namespace Attempt9 {
    public class ReferenceCheckoutStatement : IStatementSyntax {
        public IExpressionSyntax Operand { get; }

        public ReferenceCheckoutStatement(IExpressionSyntax operand) {
            this.Operand = operand;
        }

        public void Accept(IStatementVisitor visitor) => visitor.Visit(this);
    }
}