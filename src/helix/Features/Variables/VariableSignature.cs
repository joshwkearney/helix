using Helix.Analysis;
using Helix.Analysis.Types;

namespace Helix.Analysis {
    public record VariableSignature {
        public HelixType Type { get; }

        public bool IsWritable { get; }

        public IdentifierPath Path { get; }

        public Lifetime Lifetime { get; }

        public VariableSignature(IdentifierPath path, HelixType type, 
            bool isWritable, Lifetime lifetime) {

            this.Path = path;
            this.Type = type;
            this.IsWritable = isWritable;
            this.Lifetime = lifetime;
        }
    }
}