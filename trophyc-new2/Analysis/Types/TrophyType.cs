namespace Trophy.Analysis.Types {
    public abstract record TrophyType {
        public virtual Option<PointerType> AsPointerType() => new();

        public virtual Option<NamedType> AsNamedType() => new();
    }
}