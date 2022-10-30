using Helix.Analysis;
using Helix.Analysis.Types;

namespace Helix.Features.Aggregates {
    public record StructSignature {
        public IdentifierPath Path { get; }

        public IReadOnlyList<AggregateMember> Members { get; }

        public StructSignature(IdentifierPath name, IReadOnlyList<AggregateMember> mems) {
            this.Path = name;
            this.Members = mems;
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