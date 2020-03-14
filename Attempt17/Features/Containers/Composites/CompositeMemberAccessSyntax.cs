using System;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Composites {
    public class CompositeMemberAccessSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Target { get; }

        public string MemberName { get; }

        public CompositeInfo CompositeInfo { get; }

        public CompositeMemberAccessSyntax(T tag, ISyntax<T> target, string member, CompositeInfo info) {
            this.Tag = tag;
            this.Target = target;
            this.MemberName = member;
            this.CompositeInfo = info;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor
                .ContainersVisitor
                .CompositesVisitor
                .VisitCompositeMemberAccess(this, visitor, context);
        }
    }
}