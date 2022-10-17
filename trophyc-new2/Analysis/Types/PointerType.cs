namespace Trophy.Analysis.Types {
    public record PointerType : TrophyType {
        public TrophyType ReferencedType { get; }

        public bool IsWritable { get; }

        public PointerType(TrophyType innerType, bool isWritable) {
            this.ReferencedType = innerType;
            this.IsWritable = isWritable;
        }

        public override Option<PointerType> AsPointerType() => this;

        public override string ToString() {
            return this.ReferencedType + (this.IsWritable ? "*" : "^");
        }
    }
}