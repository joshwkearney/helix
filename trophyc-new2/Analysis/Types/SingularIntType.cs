using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public record SingularIntType : TrophyType {
        public int Value { get; }

        public SingularIntType(int value) {
            this.Value = value;
        }

        public override ISyntaxTree ToSyntax(TokenLocation loc) {
            return new IntLiteral(loc, this.Value);
        }

        public override bool CanUnifyTo(TrophyType other, SyntaxFrame types, bool isCast) {
            if (base.CanUnifyTo(other, types, isCast)) {
                return true;
            }

            return other == PrimitiveType.Int;
        }

        public override ISyntaxTree UnifyTo(TrophyType other, ISyntaxTree syntax, bool isCast, SyntaxFrame types) {
            if (base.CanUnifyTo(other, types, isCast)) {
                return base.UnifyTo(other, syntax, isCast, types);
            }

            // Singular ints unifying to ints do not require any syntax changes
            return syntax;
        }

        public override bool CanUnifyFrom(TrophyType other, SyntaxFrame types) {
            if (base.CanUnifyFrom(other, types)) {
                return true;
            }

            return other.CanUnifyTo(PrimitiveType.Int, types, false);
        }

        public override TrophyType UnifyFrom(TrophyType other, SyntaxFrame types) {
            if (base.CanUnifyFrom(other, types)) {
                return base.UnifyFrom(other, types);
            }

            return PrimitiveType.Int;
        }

        public override TrophyType ToMutableType() {
            return PrimitiveType.Int;
        }

        public override string ToString() => this.Value.ToString();
    }
}