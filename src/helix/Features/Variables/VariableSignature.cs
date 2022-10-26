using Helix.Analysis;
using Helix.Analysis.Types;

namespace Helix.Analysis {
    public record struct VariableSignature {
        private readonly int mutationCount;

        public HelixType Type { get; }

        public bool IsWritable { get; }

        public IdentifierPath Path { get; }

        public Lifetime Lifetime => new Lifetime(this.Path, this.mutationCount);

        public VariableSignature(IdentifierPath path, HelixType type, 
            bool isWritable, int mutationCount) {

            this.Path = path;
            this.Type = type;
            this.IsWritable = isWritable;
            this.mutationCount = mutationCount;
        }

        public VariableSignature(HelixType type,
            bool isWritable, Lifetime lifetime) {

            this.Path = lifetime.Path;
            this.Type = type;
            this.IsWritable = isWritable;
            this.mutationCount = lifetime.MutationCount;
        }
    }
}