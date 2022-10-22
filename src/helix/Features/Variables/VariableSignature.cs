using Helix.Analysis;
using Helix.Analysis.Types;

namespace Helix.Features.Variables {
    public record VariableSignature {
        public HelixType Type { get; }

        public bool IsWritable { get; }

        public IdentifierPath Path { get; }

        public VariableSignature(IdentifierPath path, HelixType type, bool isWritable) {
            Path = path;
            Type = type;
            IsWritable = isWritable;
        }
    }
}