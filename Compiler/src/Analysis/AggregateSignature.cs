using Trophy.Analysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trophy.Analysis {
    public class AggregateSignature : IEquatable<AggregateSignature> {
        public string Name { get; }

        public IReadOnlyList<StructMember> Members { get; }

        public AggregateSignature(string name, IReadOnlyList<StructMember> mems) {
            this.Name = name;
            this.Members = mems;
        }

        public bool Equals(AggregateSignature other) {
            if (other is null) {
                return false;
            }

            return this.Name == other.Name && this.Members.SequenceEqual(other.Members);
        }

        public override bool Equals(object obj) {
            return obj is AggregateSignature sig && this.Equals(sig);
        }

        public override int GetHashCode() {
            return this.Name.GetHashCode() + 32 * this.Members.Aggregate(2, (x, y) => x + 11 * y.GetHashCode());
        }
    }

    public class StructMember : IEquatable<StructMember> {
        public string MemberName { get; }

        public ITrophyType MemberType { get; }

        public StructMember(string name, ITrophyType type) {
            this.MemberName = name;
            this.MemberType = type;
        }

        public bool Equals(StructMember other) {
            if (other is null) {
                return false;
            }

            return this.MemberName == other.MemberName
                && this.MemberType.Equals(other.MemberType);
        }

        public override bool Equals(object obj) {
            if (obj is StructMember mem) {
                return this.Equals(mem);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.MemberName.GetHashCode()
                + 7 * this.MemberType.GetHashCode();
        }
    }
}