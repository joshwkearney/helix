namespace Attempt12 {
    public class VariableDefinitionSyntax : ISyntaxTree {
        public string Name { get; }

        public ISyntaxTree AssignExpression { get; }

        public ISyntaxTree ScopeExpression { get; }

        public ITrophyType ExpressionType => this.ScopeExpression.ExpressionType;

        public Scope Scope { get; }

        public bool IsConstant { get; }

        public VariableDefinitionSyntax(string name, ISyntaxTree assign, ISyntaxTree appendix, Scope env) {
            this.Name = name;
            this.AssignExpression = assign;
            this.ScopeExpression = appendix;
            this.Scope = env;
            this.IsConstant = assign.IsConstant && appendix.IsConstant;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}