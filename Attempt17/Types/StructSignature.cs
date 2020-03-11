using System;
using System.Collections.Immutable;
using Attempt17.Types;

namespace Attempt17.Types {
    public class ContainerMember {
        public string Name { get; }

        public LanguageType Type { get; }

        public ContainerMember(string name, LanguageType type) {
            this.Name = name;
            this.Type = type;
        }
    }

    public class CompositeSignature {
        public string Name { get; }

        public ImmutableList<ContainerMember> Members { get; }

        public CompositeSignature(string name, ImmutableList<ContainerMember> mems) {
            this.Name = name;
            this.Members = mems;
        }
    }
}