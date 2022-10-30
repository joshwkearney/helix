using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Features.Primitives;
using Helix.Parsing;

namespace Helix.Analysis.Types {
    public record SingularIntType : HelixType {
        public int Value { get; }

        public SingularIntType(int value) {
            this.Value = value;
        }

        public override ISyntaxTree ToSyntax(TokenLocation loc) {
            return new IntLiteral(loc, this.Value);
        }

        public override bool CanUnifyTo(HelixType other, EvalFrame types, bool isCast) {
            if (base.CanUnifyTo(other, types, isCast)) {
                return true;
            }

            return other == PrimitiveType.Int;
        }

        public override ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax, bool isCast, EvalFrame types) {
            if (base.CanUnifyTo(other, types, isCast)) {
                return base.UnifyTo(other, syntax, isCast, types);
            }

            // Singular ints unifying to ints do not require any syntax changes
            return syntax;
        }

        public override bool CanUnifyFrom(HelixType other, EvalFrame types) {
            if (base.CanUnifyFrom(other, types)) {
                return true;
            }

            return other.CanUnifyTo(PrimitiveType.Int, types, false);
        }

        public override HelixType UnifyFrom(HelixType other, EvalFrame types) {
            if (base.CanUnifyFrom(other, types)) {
                return base.UnifyFrom(other, types);
            }

            return PrimitiveType.Int;
        }

        public override HelixType ToMutableType() {
            return PrimitiveType.Int;
        }

        public override string ToString() => this.Value.ToString();

        public override bool IsRemote(EvalFrame types) => false;
    }
}