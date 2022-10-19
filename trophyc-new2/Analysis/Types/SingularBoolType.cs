using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public record SingularBoolType : TrophyType {
        public bool Value { get; }

        public SingularBoolType(bool value) {
            this.Value = value;
        }

        public override ISyntaxTree ToSyntax(TokenLocation loc) {
            return new BoolLiteral(loc, this.Value);
        }

        public override bool CanUnifyTo(TrophyType other, SyntaxFrame types, bool isCast) {
            if (base.CanUnifyTo(other, types, isCast)) {
                return true;
            }

            return other == PrimitiveType.Bool || other == PrimitiveType.Int;
        }

        public override ISyntaxTree UnifyTo(TrophyType other, ISyntaxTree syntax, bool isCast, SyntaxFrame types) {
            if (base.CanUnifyTo(other, types, isCast)) {
                return base.UnifyTo(other, syntax, isCast, types);
            }

            // Singular bools unifying to bools or ints do not require any syntax changes
            return syntax;
        }

        public override bool CanUnifyFrom(TrophyType other, SyntaxFrame types) {
            if (base.CanUnifyFrom(other, types)) {
                return true;
            }

            return other.CanUnifyTo(PrimitiveType.Bool, types, false);
        }

        public override TrophyType UnifyFrom(TrophyType other, SyntaxFrame types) {
            if (base.CanUnifyFrom(other, types)) {
                return base.UnifyFrom(other, types);
            }

            return PrimitiveType.Bool;
        }

        public override TrophyType ToMutableType() {
            return PrimitiveType.Bool;
        }

        public override string ToString() => this.Value.ToString().ToLower();
    }
}