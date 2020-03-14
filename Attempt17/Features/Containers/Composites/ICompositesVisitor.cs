using System;
namespace Attempt17.Features.Containers.Composites {
    public interface ICompositesVisitor<T, TTag, TContext> {
        public T VisitCompositeDeclaration(CompositeDeclarationSyntax<TTag> syntax,
            ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitNewComposite(NewCompositeSyntax<TTag> syntax,
            ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitCompositeMemberAccess(CompositeMemberAccessSyntax<TTag> syntax,
            ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);
    }
}
