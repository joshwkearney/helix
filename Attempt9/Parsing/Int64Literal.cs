namespace Attempt9 {
    public class Int64Literal : IParseTree {
        public long Value { get; }

        public Int64Literal(long value) {
            this.Value = value;
        }

        public void Accept(IParseTreeVisitor visitor) => visitor.Visit(this);
    }
}