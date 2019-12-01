namespace Attempt12 {
    public class IfExpressionSyntax : ISyntaxTree {
        public ISyntaxTree Condition { get; }

        public ISyntaxTree AffirmativeExpression { get; }

        public ISyntaxTree NegativeExpression { get; }

        public ITrophyType ExpressionType { get; }

        public Scope Scope { get; }

        public bool IsConstant { get; }

        public IfExpressionSyntax(ISyntaxTree condition, ISyntaxTree affirm, ISyntaxTree neg, ITrophyType returnType, Scope env) {
            this.Condition = condition;
            this.AffirmativeExpression = affirm;
            this.NegativeExpression = neg;
            this.ExpressionType = returnType;
            this.Scope = env;

            this.IsConstant = condition.IsConstant && affirm.IsConstant && neg.IsConstant;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}