using System;
using System.Collections.Immutable;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Containers {
    public class NewCompositeSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public CompositeInfo CompositeInfo { get; }

        public ImmutableList<MemberInstantiation<T>> Instantiations { get; }

        public NewCompositeSyntax(T tag, CompositeInfo info, ImmutableList<MemberInstantiation<T>> insts) {
            this.Tag = tag;
            this.CompositeInfo = info;
            this.Instantiations = insts;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor
                .ContainersVisitor
                .CompositesVisitor
                .VisitNewComposite(this, visitor, context);
        }
    }
}