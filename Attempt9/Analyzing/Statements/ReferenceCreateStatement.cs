namespace Attempt9 {
    public class ReferenceCreateStatement : IStatementSyntax {
        public string ResultName { get; }

        public IExpressionSyntax Operand { get; }

        public ReferenceCreateStatement(string name, IExpressionSyntax operand) {
            this.ResultName = name;
            this.Operand = operand;
        }

        public void Accept(IStatementVisitor visitor) => visitor.Visit(this);
    }
}