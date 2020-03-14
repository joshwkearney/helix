using System;
namespace Attempt17.Features.Containers.Unions {
    public interface IUnionVisitor<T, TTag, TContext> {
        public T VisitUnionDeclaration(UnionDeclarationSyntax<TTag> syntax,
            ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);

        public T VisitNewUnion(NewUnionSyntax<TTag> syntax,
            ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);
    }
}
