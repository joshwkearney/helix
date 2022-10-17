namespace Trophy.Analysis.Types {
    public record NamedType : TrophyType {
        public IdentifierPath FullName { get; } 

        public NamedType(IdentifierPath fullName) {
            this.FullName = fullName;
        }

        public override string ToString() {
            return this.FullName.Segments.Last();
        }

        public override IEnumerable<TrophyType> GetContainedValueTypes(ITypesRecorder types) {
            var target = types.TryResolveName(this.FullName).GetValue();

            if (target == NameTarget.Aggregate) {
                var sig = types.GetAggregate(this.FullName);

                return sig.Members
                    .SelectMany(x => x.MemberType.GetContainedValueTypes(types))
                    .Prepend(this);
            }

            return Array.Empty<TrophyType>();
        }
    }
}