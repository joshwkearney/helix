namespace Helix.Analysis.Types {
    public record NamedType : HelixType {
        public IdentifierPath Path { get; } 

        public NamedType(IdentifierPath fullName) {
            this.Path = fullName;
        }

        public override string ToString() {
            return this.Path.Segments.Last();
        }

        public override IEnumerable<HelixType> GetContainedValueTypes(SyntaxFrame types) {
            if (types.Aggregates.TryGetValue(this.Path, out var sig)) {

                return sig.Members
                    .SelectMany(x => x.Type.GetContainedValueTypes(types))
                    .Prepend(this);
            }

            return Array.Empty<HelixType>();
        }
    }
}