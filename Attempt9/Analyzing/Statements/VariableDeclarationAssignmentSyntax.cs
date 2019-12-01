namespace Attempt9 {
    public class VariableDeclarationAssignmentSyntax : IStatementSyntax {
        public string Name { get; }

        public IExpressionSyntax AssignSyntax { get; }

        public VariableDeclarationAssignmentSyntax(string name, IExpressionSyntax assign) {
            this.Name = name;
            this.AssignSyntax = assign;
        }

        public void Accept(IStatementVisitor visitor) => visitor.Visit(this);
    }
}