using Trophy.Analysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trophy.Parsing {
    public class CompositeSignature : IEquatable<CompositeSignature> {
        public string Name { get; }

        public IReadOnlyList<Parameter> Members { get; }

        public CompositeSignature(string name, IReadOnlyList<Parameter> mems) {
            this.Name = name;
            this.Members = mems;
        }

        public bool Equals(CompositeSignature other) {
            if (other is null) {
                return false;
            }

            return this.Name == other.Name && this.Members.SequenceEqual(other.Members);
        }

        public override bool Equals(object obj) {
            return obj is CompositeSignature sig && this.Equals(sig);
        }

        public override int GetHashCode() {
            return this.Name.GetHashCode() + 32 * this.Members.Aggregate(2, (x, y) => x + 11 * y.GetHashCode());
        }
    }
}