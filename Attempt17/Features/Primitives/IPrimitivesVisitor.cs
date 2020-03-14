using System;
namespace Attempt17.Features.Primitives {
    public interface IPrimitivesVisitor<T, TTag, TContext> {
        public T VisitAs(AsSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitAlloc(AllocSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitBinary(BinarySyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitBoolLiteral(BoolLiteralSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitIntLiteral(IntLiteralSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitVoidLiteral(VoidLiteralSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);
    }
}