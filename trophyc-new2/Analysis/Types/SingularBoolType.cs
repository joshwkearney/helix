using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public record SingularBoolType : TrophyType {
        public bool Value { get; }

        public SingularBoolType(bool value) {
            this.Value = value;
        }

        public override bool CanUnifyTo(TrophyType other) {
            if (base.CanUnifyTo(other)) {
                return true;
            }

            return other == PrimitiveType.Bool;
        }

        public override ISyntax UnifyTo(TrophyType other, ISyntax syntax) {
            if (base.CanUnifyTo(other)) {
                return base.UnifyTo(other, syntax);
            }

            // Singular bools unifying to bools do not require any syntax changes
            return syntax;
        }

        public override bool CanUnifyFrom(TrophyType other) {
            if (base.CanUnifyFrom(other)) {
                return true;
            }

            return other.CanUnifyTo(PrimitiveType.Bool);
        }

        public override TrophyType UnifyFrom(TrophyType other) {
            if (base.CanUnifyFrom(other)) {
                return base.UnifyFrom(other);
            }

            return PrimitiveType.Bool;
        }

        public override TrophyType RemoveDependentTyping() {
            return PrimitiveType.Bool;
        }

        public override string ToString() => this.Value.ToString();
    }
}