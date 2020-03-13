using System;
using System.Collections.Immutable;
using Attempt17.Types;

namespace Attempt17 {
    public class CompositeSignature {
        public string Name { get; }

        public ImmutableList<Parameter> Members { get; }

        public CompositeSignature(string name, ImmutableList<Parameter> mems) {
            this.Name = name;
            this.Members = mems;
        }
    }
}