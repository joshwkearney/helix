namespace Attempt9 {
    public class VariableDefinition : IParseTree {
        public string Name { get; }

        public IParseTree AssignExpression { get; }

        public IParseTree ScopeExpression { get; }

        public VariableDefinition(string name, IParseTree assign, IParseTree scope) {
            this.Name = name;
            this.AssignExpression = assign;
            this.ScopeExpression = scope;
        }

        public void Accept(IParseTreeVisitor visitor) => visitor.Visit(this);
    }
}