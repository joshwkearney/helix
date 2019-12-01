namespace Attempt9 {
    public enum UnaryOperator {
        Negation,
        Not,
        Box, Unbox
    }

    public class UnaryExpression : IParseTree {
        public IParseTree Expression { get; }

        public UnaryOperator Operator { get; }

        public UnaryExpression(IParseTree expr, UnaryOperator op) {
            this.Expression = expr;
            this.Operator = op;
        }

        public void Accept(IParseTreeVisitor visitor) => visitor.Visit(this);
    }
}