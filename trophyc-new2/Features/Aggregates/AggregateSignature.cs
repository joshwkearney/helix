using Trophy.Analysis;
using Trophy.Analysis.Types;

namespace Trophy.Features.Aggregates {
    public record AggregateSignature {
        public IdentifierPath Path { get; }

        public IReadOnlyList<AggregateMember> Members { get; }

        public AggregateKind Kind { get; }

        public AggregateSignature(IdentifierPath name, AggregateKind kind,
            IReadOnlyList<AggregateMember> mems) {

            this.Path = name;
            this.Members = mems;
            this.Kind = kind;
        }                
    }

    public record AggregateMember {
        public string MemberName { get; }

        public TrophyType MemberType { get; }

        public bool IsWritable { get; }

        public AggregateMember(string name, TrophyType type, bool isWritable) {
            this.MemberName = name;
            this.MemberType = type;
            this.IsWritable = isWritable;
        }
    }
}