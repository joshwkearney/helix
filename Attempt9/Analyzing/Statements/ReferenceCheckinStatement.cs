namespace Attempt9 {
    public class ReferenceCheckinStatement : IStatementSyntax {
        public IExpressionSyntax Operand { get; }

        public ReferenceCheckinStatement(IExpressionSyntax operand) {
            this.Operand = operand;
        }

        public void Accept(IStatementVisitor visitor) => visitor.Visit(this);
    }
}