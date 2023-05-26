using Helix.Analysis.TypeChecking;
using Helix.Syntax;
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

        public override HelixType GetMutationSupertype(ITypedFrame types) {
            return PrimitiveType.Bool;
        }

        public override HelixType GetSignatureSupertype(ITypedFrame types) {
            return PrimitiveType.Bool;
        }

        public override ISyntaxTree ToSyntax(TokenLocation loc) {
            return new BoolLiteral(loc, this.Value);
        }      

        public override string ToString() => this.Value.ToString().ToLower();
    }
}