using Helix.Analysis;
using Helix.Analysis.Types;

namespace Helix.Features.Aggregates {
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
        public string Name { get; }

        public HelixType Type { get; }

        public bool IsWritable { get; }

        public AggregateMember(string name, HelixType type, bool isWritable) {
            this.Name = name;
            this.Type = type;
            this.IsWritable = isWritable;
        }
    }
}