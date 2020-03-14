using System;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Unions {
    public class NewUnionSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public CompositeInfo CompositeInfo { get; }

        public MemberInstantiation<T> Instantiation { get; }

        public NewUnionSyntax(T tag, CompositeInfo info, MemberInstantiation<T> inst) {
            this.Tag = tag;
            this.CompositeInfo = info;
            this.Instantiation = inst;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor
                .ContainersVisitor
                .UnionVisitor
                .VisitNewUnion(this, visitor, context);
        }
    }
}
