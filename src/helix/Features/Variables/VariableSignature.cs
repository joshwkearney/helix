using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Parsing;

namespace Helix.Features.Variables {
    public record Lifetime(bool IsStackBound, 
                           IReadOnlyList<IdentifierPath> Origins, 
                           IReadOnlyList<ISyntaxTree> Values) {

        public Lifetime() : this(false, Array.Empty<IdentifierPath>(), Array.Empty<ISyntaxTree>()) { }

        public Lifetime Merge(Lifetime other) {
            var automatic = this.IsStackBound || other.IsStackBound;
            var origins = this.Origins.Concat(other.Origins).ToArray();
            var values = this.Values.Concat(other.Values).ToArray();

            return new Lifetime(automatic, origins, values);
        }

        public Lifetime WithStackBinding(bool isStackBound) {
            return new Lifetime(isStackBound, this.Origins, this.Values);
        }

        public bool HasCompatibleOrigins(Lifetime assignValue) {
            //if (!this.IsStackBound && assignValue.IsStackBound) {
            //    return false;
            //}

            return !assignValue.Origins.Except(this.Origins).Any();
        }
    }

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