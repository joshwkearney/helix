namespace Attempt10 {
    public class IfExpressionSyntax : ISyntaxTree {
        public ISyntaxTree Condition { get; }

        public ISyntaxTree AffirmativeExpression { get; }

        public ISyntaxTree NegativeExpression { get; }

        public ITrophyType ExpressionType { get; }

        public Scope Scope { get; }

        public IfExpressionSyntax(ISyntaxTree condition, ISyntaxTree affirm, ISyntaxTree neg, ITrophyType returnType, Scope scope) {
            this.Condition = condition;
            this.AffirmativeExpression = affirm;
            this.NegativeExpression = neg;
            this.ExpressionType = returnType;
            this.Scope = scope;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}