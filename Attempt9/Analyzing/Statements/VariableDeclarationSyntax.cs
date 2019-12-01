namespace Attempt9 {
    public class VariableDeclarationSyntax : IStatementSyntax {
        public string Name { get; }

        public ITrophyType VariableType { get; }

        public VariableDeclarationSyntax(string name, ITrophyType type) {
            this.Name = name;
            this.VariableType = type;
        }

        public void Accept(IStatementVisitor visitor) => visitor.Visit(this);
    }
}