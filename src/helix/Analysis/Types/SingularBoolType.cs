using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Features.Primitives;
using Helix.Parsing;

namespace Helix.Analysis.Types {
    public record SingularBoolType : HelixType {
        public bool Value { get; }

        public SingularBoolType(bool value) {
            this.Value = value;
        }

        public override ISyntaxTree ToSyntax(TokenLocation loc) {
            return new BoolLiteral(loc, this.Value);
        }

        public override bool CanUnifyTo(HelixType other, EvalFrame types, bool isCast) {
            if (base.CanUnifyTo(other, types, isCast)) {
                return true;
            }

            return other == PrimitiveType.Bool || other == PrimitiveType.Int;
        }

        public override ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax, bool isCast, EvalFrame types) {
            if (base.CanUnifyTo(other, types, isCast)) {
                return base.UnifyTo(other, syntax, isCast, types);
            }

            // Singular bools unifying to bools or ints do not require any syntax changes
            return syntax;
        }

        public override bool CanUnifyFrom(HelixType other, EvalFrame types) {
            if (base.CanUnifyFrom(other, types)) {
                return true;
            }

            return other.CanUnifyTo(PrimitiveType.Bool, types, false);
        }

        public override HelixType UnifyFrom(HelixType other, EvalFrame types) {
            if (base.CanUnifyFrom(other, types)) {
                return base.UnifyFrom(other, types);
            }

            return PrimitiveType.Bool;
        }

        public override HelixType ToMutableType() {
            return PrimitiveType.Bool;
        }

        public override string ToString() => this.Value.ToString().ToLower();

        public override bool IsRemote(EvalFrame types) => false;
    }
}