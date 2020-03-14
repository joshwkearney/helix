using Attempt17.Features.Containers.Composites;
using Attempt17.Features.Containers.Unions;
using Attempt17.Features.Functions;
using Attempt17.TypeChecking;

namespace Attempt17.Features  {
    public interface IDeclarationVisitor<T, TTag> {
        public T VisitFunctionDeclaration(FunctionDeclarationSyntax<TTag> decl,
            ITypeCheckScope scope);

        public T VisitCompositeDeclaration(CompositeDeclarationSyntax<TTag> decl,
            ITypeCheckScope scope);

        public T VisitUnionDeclaration(UnionDeclarationSyntax<TTag> decl,
            ITypeCheckScope scope);
    }
}