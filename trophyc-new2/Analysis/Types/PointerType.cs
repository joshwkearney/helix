using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public record PointerType : TrophyType {
        public TrophyType ReferencedType { get; }

        public bool IsWritable { get; }

        public PointerType(TrophyType innerType, bool isWritable) {
            this.ReferencedType = innerType;
            this.IsWritable = isWritable;
        }

        public override bool CanUnifyTo(TrophyType other, SyntaxFrame types, bool isCast) {
            if (this == other) {
                return true;
            }

            if (other is PointerType pointer && !pointer.IsWritable) {
                return true;
            }

            return false;
        }

        public override ISyntaxTree UnifyTo(TrophyType other, ISyntaxTree syntax, bool isCast, SyntaxFrame types) => syntax;

        public override string ToString() {
            return this.ReferencedType + (this.IsWritable ? "*" : "^");
        }
    }
}