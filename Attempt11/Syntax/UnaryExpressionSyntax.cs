namespace Attempt10 {
    public enum UnaryOperator {
        Negation, Not
    }

    public class UnaryExpressionSyntax : ISyntaxTree {
        public ISyntaxTree Operand { get; }

        public UnaryOperator Operator { get; }

        public ITrophyType ExpressionType { get; }

        public Scope Scope { get; }

        public UnaryExpressionSyntax(ISyntaxTree operand, UnaryOperator op, ITrophyType returnType, Scope env) {
            this.Operand = operand;
            this.Operator = op;
            this.ExpressionType = returnType;
            this.Scope = env;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}