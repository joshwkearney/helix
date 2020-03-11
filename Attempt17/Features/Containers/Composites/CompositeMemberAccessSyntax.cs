using System;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Composites {
    public class CompositeMemberAccessSyntax : ISyntax<TypeCheckTag> {
        public TypeCheckTag Tag { get; }

        public ISyntax<TypeCheckTag> Target { get; }

        public string MemberName { get; }

        public CompositeInfo CompositeInfo { get; }

        public CompositeMemberAccessSyntax(TypeCheckTag tag, ISyntax<TypeCheckTag> target, string member, CompositeInfo info) {
            this.Tag = tag;
            this.Target = target;
            this.MemberName = member;
            this.CompositeInfo = info;
        }
    }
}