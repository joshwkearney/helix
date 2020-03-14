namespace Attempt17.Features.Primitives {
    public class IntLiteralSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public long Value { get; }

        public IntLiteralSyntax(T tag, long value) {
            this.Tag = tag;
            this.Value = value;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.PrimitivesVisitor.VisitIntLiteral(this, visitor, context);
        }
    }
}