namespace Attempt10 {
    public class VariableLiteralSyntax : ISyntaxTree {
        public string Name { get; }

        public ITrophyType ExpressionType { get; }

        public Scope Scope { get; }

        public VariableLiteralSyntax(string name, ITrophyType returnType, Scope env) {
            this.Name = name;
            this.ExpressionType = returnType;
            this.Scope = env;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}