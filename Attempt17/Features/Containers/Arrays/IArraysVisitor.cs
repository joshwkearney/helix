namespace Attempt17.Features.Containers.Arrays {
    public interface IArraysVisitor<T, TTag, TContext> {
        public T VisitIndex(ArrayIndexSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitLiteral(ArrayLiteralSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitArrayRangeLiteral(ArrayRangeLiteralSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitSizeAccess(ArraySizeAccessSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitStore(ArrayStoreSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);
    }
}
