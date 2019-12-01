namespace Attempt10 {
    public class VariableDefinitionSyntax : ISyntaxTree {
        public string Name { get; }

        public ISyntaxTree AssignExpression { get; }

        public ISyntaxTree ScopeExpression { get; }

        public ITrophyType ExpressionType => this.ScopeExpression.ExpressionType;

        public Scope Scope { get; }

        public VariableDefinitionSyntax(string name, ISyntaxTree assign, ISyntaxTree appendix, Scope env) {
            this.Name = name;
            this.AssignExpression = assign;
            this.ScopeExpression = appendix;
            this.Scope = env;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}