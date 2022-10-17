using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public record PointerType : TrophyType {
        public TrophyType ReferencedType { get; }

        public bool IsWritable { get; }

        public PointerType(TrophyType innerType, bool isWritable) {
            this.ReferencedType = innerType;
            this.IsWritable = isWritable;
        }

        public override Option<PointerType> AsPointerType() => this;

        public override bool CanUnifyWith(TrophyType other) {
            if (this == other) {
                return true;
            }

            if (other is PointerType pointer && !pointer.IsWritable) {
                return true;
            }

            return false;
        }

        public override ISyntax UnifyTo(TrophyType other, ISyntax syntax) => syntax;

        public override string ToString() {
            return this.ReferencedType + (this.IsWritable ? "*" : "^");
        }
    }
}