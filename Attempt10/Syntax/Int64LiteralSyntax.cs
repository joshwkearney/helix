namespace Attempt12 {
    public class Int64LiteralSyntax : ISyntaxTree {
        public long Value { get; }

        public ITrophyType ExpressionType => PrimitiveTrophyType.Int64Type;

        public Scope Scope { get; }

        public bool IsConstant => true;

        public Int64LiteralSyntax(long value, Scope env) {
            this.Value = value;
            this.Scope = env;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}