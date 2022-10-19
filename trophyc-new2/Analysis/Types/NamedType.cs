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
            var target = types.TryResolveName(this.Path).GetValue();

            if (target == NameTarget.Aggregate) {
                var sig = types.GetAggregate(this.Path);

                return sig.Members
                    .SelectMany(x => x.Type.GetContainedValueTypes(types))
                    .Prepend(this);
            }

            return Array.Empty<TrophyType>();
        }
    }
}