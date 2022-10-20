using Trophy.Features.Arrays;
using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public record ArrayType : TrophyType {
        public TrophyType InnerType { get; }

        public ArrayType(TrophyType innerType) {
            this.InnerType = innerType;
        }

        public override bool CanUnifyTo(TrophyType other, SyntaxFrame types, bool isCast) {
            if (other is PointerType pointer && pointer.IsWritable) {
                if (this.InnerType.Equals(pointer.InnerType)) {
                    return true;
                }
            }

            return base.CanUnifyTo(other, types, isCast);
        }

        public override ISyntaxTree UnifyTo(TrophyType other, ISyntaxTree syntax, bool isCast, SyntaxFrame types) {
            if (other is PointerType) {
                return new ArrayToPointerAdapter(this, syntax);
            }

            return base.UnifyTo(other, syntax, isCast, types);
        }

        public override string ToString() {
            return this.InnerType + "[]";
        }
    }
}