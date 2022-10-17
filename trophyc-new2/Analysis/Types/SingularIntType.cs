using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public record SingularIntType : TrophyType {
        public int Value { get; }

        public SingularIntType(int value) {
            this.Value = value;
        }

        public override bool CanUnifyTo(TrophyType other) {
            if (base.CanUnifyTo(other)) {
                return true;
            }

            return other == PrimitiveType.Int;
        }

        public override ISyntax UnifyTo(TrophyType other, ISyntax syntax) {
            if (base.CanUnifyTo(other)) {
                return base.UnifyTo(other, syntax);
            }

            // Singular ints unifying to ints do not require any syntax changes
            return syntax;
        }

        public override bool CanUnifyFrom(TrophyType other) {
            if (base.CanUnifyFrom(other)) {
                return true;
            }

            return other.CanUnifyTo(PrimitiveType.Int);
        }

        public override TrophyType UnifyFrom(TrophyType other) {
            if (base.CanUnifyFrom(other)) {
                return base.UnifyFrom(other);
            }

            return PrimitiveType.Int;
        }

        public override TrophyType ToMutableType() {
            return PrimitiveType.Int;
        }

        public override string ToString() => this.Value.ToString();
    }
}