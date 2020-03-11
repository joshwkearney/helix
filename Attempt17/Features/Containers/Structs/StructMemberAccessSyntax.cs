using System;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Structs {
    public class StructMemberAccessSyntax : ISyntax<TypeCheckTag> {
        public TypeCheckTag Tag { get; }

        public ISyntax<TypeCheckTag> Target { get; }

        public string MemberName { get; }

        public StructMemberAccessSyntax(TypeCheckTag tag, ISyntax<TypeCheckTag> target, string member) {
            this.Tag = tag;
            this.Target = target;
            this.MemberName = member;
        }
    }
}