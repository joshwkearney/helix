using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;

namespace Helix.Analysis {
    public record struct VariableSignature {
        public HelixType Type { get; }

        public bool IsWritable { get; }

        public IdentifierPath Path { get; }

        public Lifetime Lifetime { get; }

        public VariableSignature(IdentifierPath path, HelixType type, 
            bool isWritable, int mutationCount, bool isRoot) {

            this.Path = path;
            this.Type = type;
            this.IsWritable = isWritable;
            this.Lifetime = new Lifetime(this.Path, mutationCount, isRoot);
        }

        public VariableSignature(HelixType type,
            bool isWritable, Lifetime lifetime) {

            this.Path = lifetime.Path;
            this.Type = type;
            this.IsWritable = isWritable;
            this.Lifetime = lifetime;
        }
    }
}