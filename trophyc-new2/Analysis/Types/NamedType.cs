namespace Trophy.Analysis.Types {
    public record NamedType : TrophyType {
        public IdentifierPath Path { get; } 

        public NamedType(IdentifierPath fullName) {
            this.Path = fullName;
        }

        public override string ToString() {
            return this.Path.Segments.Last();
        }

        public override IEnumerable<TrophyType> GetContainedValueTypes(ITypesRecorder types) {
            if (types.TryGetAggregate(this.Path).TryGetValue(out var sig)) {

                return sig.Members
                    .SelectMany(x => x.Type.GetContainedValueTypes(types))
                    .Prepend(this);
            }

            return Array.Empty<TrophyType>();
        }
    }
}