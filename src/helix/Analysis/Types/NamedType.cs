namespace Helix.Analysis.Types {
    public record NamedType : HelixType {
        public IdentifierPath Path { get; } 

        public NamedType(IdentifierPath fullName) {
            this.Path = fullName;
        }

        public override string ToString() {
            return this.Path.Segments.Last();
        }

        public override IEnumerable<HelixType> GetContainedTypes(EvalFrame types) {
            if (types.Aggregates.TryGetValue(this.Path, out var sig)) {
                return sig.Members
                    .SelectMany(x => x.Type.GetContainedTypes(types))
                    .Prepend(this);
            }

            return new[] { this };
        }

        public override bool IsRemote(EvalFrame types) {
            if (types.Functions.ContainsKey(this.Path)) {
                return false;
            }

            if (types.Aggregates.TryGetValue(this.Path, out var sig)) {
                return false;
            }

            throw new InvalidOperationException("Unexpected named type");
        }
    }
}