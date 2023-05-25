using Helix.Analysis;
using Helix.Analysis.Types;

namespace Helix.Features.Aggregates {
    public record StructSignature {
        public IdentifierPath Path { get; }

        public IReadOnlyList<StructMember> Members { get; }

        public StructSignature(IdentifierPath name, IReadOnlyList<StructMember> mems) {
            this.Path = name;
            this.Members = mems;
        }                
    }

    public record StructMember {
        public string Name { get; }

        public HelixType Type { get; }

        public bool IsWritable { get; }

        public StructMember(string name, HelixType type, bool isWritable) {
            this.Name = name;
            this.Type = type;
            this.IsWritable = isWritable;
        }
    }
}