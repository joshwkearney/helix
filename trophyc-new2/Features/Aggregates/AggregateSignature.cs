namespace Trophy.Features.Aggregates {
    public class AggregateSignature : IEquatable<AggregateSignature> {
        public IdentifierPath Path { get; }

        public IReadOnlyList<AggregateMember> Members { get; }

        public AggregateSignature(IdentifierPath name, IReadOnlyList<AggregateMember> mems) {
            this.Path = name;
            this.Members = mems;
        }

        public bool Equals(AggregateSignature? other) {
            if (other is null) {
                return false;
            }

            return this.Path == other.Path && this.Members.SequenceEqual(other.Members);
        }

        public override bool Equals(object? obj) {
            return obj is AggregateSignature sig && this.Equals(sig);
        }

        public override int GetHashCode() {
            return this.Path.GetHashCode() + 32 * this.Members.Aggregate(2, (x, y) => x + 11 * y.GetHashCode());
        }
    }

    public class AggregateMember : IEquatable<AggregateMember> {
        public string MemberName { get; }

        public TrophyType MemberType { get; }

        public AggregateMember(string name, TrophyType type) {
            this.MemberName = name;
            this.MemberType = type;
        }

        public bool Equals(AggregateMember? other) {
            if (other is null) {
                return false;
            }

            return this.MemberName == other.MemberName
                && this.MemberType.Equals(other.MemberType);
        }

        public override bool Equals(object? obj) {
            if (obj is AggregateMember mem) {
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