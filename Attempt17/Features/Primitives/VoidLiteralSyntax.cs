namespace Attempt17.Features.Primitives {
    public class VoidLiteralSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public VoidLiteralSyntax(T tag) {
            this.Tag = tag;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.PrimitivesVisitor.VisitVoidLiteral(this, visitor, context);
        }
    }
}