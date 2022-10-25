using Helix.Analysis;
using Helix.Analysis.Types;

namespace Helix.Analysis {
    public record VariableSignature {
        public HelixType Type { get; }

        public bool IsWritable { get; }

        public IdentifierPath Path { get; }

        public int MutationCount { get; }

        public bool IsLifetimeRoot { get; }

        public VariableSignature(IdentifierPath path, HelixType type, 
            bool isWritable, int mutationCount, bool isRoot) {

            this.Path = path;
            this.Type = type;
            this.IsWritable = isWritable;
            this.MutationCount = mutationCount;
            this.IsLifetimeRoot = isRoot;
        }
    }
}