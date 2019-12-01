namespace Attempt9 {
    public class VariableAssignmentSyntax : IStatementSyntax {
        public string Name { get; }

        public IExpressionSyntax AssignExpression { get; }

        public VariableAssignmentSyntax(string name, IExpressionSyntax assign) {
            this.Name = name;
            this.AssignExpression = assign;
        }

        public void Accept(IStatementVisitor visitor) => visitor.Visit(this);
    }
}