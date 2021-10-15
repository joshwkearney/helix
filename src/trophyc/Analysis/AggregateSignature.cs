using Trophy.Analysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trophy.Analysis {
    public class AggregateSignature : IEquatable<AggregateSignature> {
        public string Name { get; }

        public IReadOnlyList<AggregateMember> Members { get; }

        public AggregateSignature(string name, IReadOnlyList<AggregateMember> mems) {
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

    public class AggregateMember : IEquatable<AggregateMember> {
        public string MemberName { get; }

        public ITrophyType MemberType { get; }

        public VariableKind Kind { get; }

        public AggregateMember(string name, ITrophyType type, VariableKind kind) {
            this.MemberName = name;
            this.MemberType = type;
            this.Kind = kind;
        }

        public bool Equals(AggregateMember other) {
            if (other is null) {
                return false;
            }

            return this.MemberName == other.MemberName
                && this.MemberType.Equals(other.MemberType)
                && this.Kind == other.Kind;
        }

        public override bool Equals(object obj) {
            if (obj is AggregateMember mem) {
                return this.Equals(mem);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.MemberName.GetHashCode()
                + 7 * this.MemberType.GetHashCode()
                + 11 * this.Kind.GetHashCode();
        }
    }
}