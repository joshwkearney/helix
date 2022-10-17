namespace Trophy.Analysis.Types {
    public record NamedType : TrophyType {
        public IdentifierPath FullName { get; } 

        public NamedType(IdentifierPath fullName) {
            this.FullName = fullName;
        }

        public override string ToString() {
            return this.FullName.Segments.Last();
        }
    }
}