using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Features.Primitives;
using Helix.Parsing;

namespace Helix.Analysis.Types {
    public record SingularIntType : HelixType {
        public int Value { get; }

        public SingularIntType(int value) {
            this.Value = value;
        }

        public override PassingSemantics GetSemantics(ITypedFrame types) {
            return PassingSemantics.ValueType;
        }

        public override ISyntaxTree ToSyntax(TokenLocation loc) {
            return new IntLiteral(loc, this.Value);
        }

        public override UnificationKind TestUnification(HelixType other, TypeFrame types) {
            return PrimitiveType.Int.TestUnification(other, types);
        }

        public override ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax, UnificationKind unify, TypeFrame types) {
            return PrimitiveType.Int.UnifyTo(other, syntax, unify, types);
        }

        public override HelixType ToMutableType() {
            return PrimitiveType.Int;
        }

        public override string ToString() => this.Value.ToString();
    }
}