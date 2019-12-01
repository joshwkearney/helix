namespace Attempt9 {
    public class Int64LiteralSyntax : IExpressionSyntax {
        public long Value { get; }

        public ITrophyType ReturnType => PrimitiveTrophyType.Int64Type;

        public Int64LiteralSyntax(long value) {
            this.Value = value;
        }

        public void Accept(IExpressionVisitor visitor) => visitor.Visit(this);
    }
}