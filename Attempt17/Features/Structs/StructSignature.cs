using System;
using System.Collections.Immutable;
using Attempt17.Types;

namespace Attempt17.Features.Structs {
    public class StructMember {
        public string Name { get; }

        public LanguageType Type { get; }

        public StructMember(string name, LanguageType type) {
            this.Name = name;
            this.Type = type;
        }
    }

    public class StructSignature {
        public string Name { get; }

        public ImmutableList<StructMember> Members { get; }

        public StructSignature(string name, ImmutableList<StructMember> mems) {
            this.Name = name;
            this.Members = mems;
        }
    }
}