namespace Attempt10 {
    public class Int64LiteralSyntax : ISyntaxTree {
        public long Value { get; }

        public ITrophyType ExpressionType => PrimitiveTrophyType.Int64Type;

        public Scope Scope { get; }

        public Int64LiteralSyntax(long value, Scope scope) {
            this.Value = value;
            this.Scope = scope;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}