namespace Attempt9 {
    public enum BinaryOperator {
        Add, Subtract, Multiply, Divide
    }

    public class BinaryExpression : IParseTree {
        public IParseTree Left { get; }

        public IParseTree Right { get; }

        public BinaryOperator Operator { get; }

        public BinaryExpression(IParseTree left, IParseTree right, BinaryOperator op) {
            this.Left = left;
            this.Right = right;
            this.Operator = op;
        }

        public void Accept(IParseTreeVisitor visitor) => visitor.Visit(this);
    }
}