using System.Collections.Immutable;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Unions {
    public class ParseUnionDeclarationSyntax<T> : IDeclaration<T> {
        public T Tag { get; }

        public CompositeInfo UnionInfo { get; }

        public ImmutableList<FunctionSignature> Methods { get; }

        public ParseUnionDeclarationSyntax(T tag, CompositeInfo info,
            ImmutableList<FunctionSignature> methods) {

            this.Tag = tag;
            this.UnionInfo = info;
            this.Methods = methods;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor
                .ContainersVisitor
                .UnionVisitor
                .VisitParseUnionDeclaration(this, visitor, context);
        }

        public T1 Accept<T1>(IDeclarationVisitor<T1, T> visitor, ITypeCheckScope scope) {
            return visitor.VisitUnionDeclaration(this, scope);
        }
    }
}