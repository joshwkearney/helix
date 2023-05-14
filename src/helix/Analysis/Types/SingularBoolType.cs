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

        public override PassingSemantics GetSemantics(ITypedFrame types) {
            return PassingSemantics.ValueType;
        }

        public override ISyntaxTree ToSyntax(TokenLocation loc) {
            return new BoolLiteral(loc, this.Value);
        }

        public override UnificationKind TestUnification(HelixType other, EvalFrame types) {
            return PrimitiveType.Bool.TestUnification(other, types);
        }

        public override ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax, UnificationKind unify, EvalFrame types) {
            return PrimitiveType.Bool.UnifyTo(other, syntax, unify, types);
        }

        public override HelixType ToMutableType() {
            return PrimitiveType.Bool;
        }

        public override string ToString() => this.Value.ToString().ToLower();
    }
}