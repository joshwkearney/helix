using System;
using System.Collections.Immutable;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Composites {
    public class CompositeDeclarationSyntax<T> : IDeclaration<T> {
        public T Tag { get; }

        public CompositeInfo CompositeInfo { get; }

        public ImmutableList<IDeclaration<T>> InnerDeclarations { get; }

        public CompositeDeclarationSyntax(T tag, CompositeInfo info,
            ImmutableList<IDeclaration<T>> innerDecls) {

            this.Tag = tag;
            this.CompositeInfo = info;
            this.InnerDeclarations = innerDecls;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor
                .ContainersVisitor
                .CompositesVisitor
                .VisitCompositeDeclaration(this, visitor, context);
        }

        public T1 Accept<T1>(IDeclarationVisitor<T1, T> visitor, ITypeCheckScope scope) {
            return visitor.VisitCompositeDeclaration(this, scope);
        }
    }
}