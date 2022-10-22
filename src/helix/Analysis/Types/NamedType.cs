namespace Helix.Analysis.Types {
    public record NamedType : HelixType {
        public IdentifierPath Path { get; } 

        public NamedType(IdentifierPath fullName) {
            this.Path = fullName;
        }

        public override string ToString() {
            return this.Path.Segments.Last();
        }

        public override IEnumerable<HelixType> GetContainedTypes(SyntaxFrame types) {
            if (types.Aggregates.TryGetValue(this.Path, out var sig)) {
                return sig.Members
                    .SelectMany(x => x.Type.GetContainedTypes(types))
                    .Prepend(this);
            }

            return new[] { this };
        }

        public override bool IsValueType(SyntaxFrame types) {
            if (types.Functions.ContainsKey(this.Path)) {
                return false;
            }

            if (types.Aggregates.TryGetValue(this.Path, out var sig)) {
                return sig.Members
                    .Select(x => x.Type)
                    .All(x => x.IsValueType(types));
            }

            throw new InvalidOperationException("Unexpected named type");
        }
    }
}