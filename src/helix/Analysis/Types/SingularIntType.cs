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

        public override PassingSemantics GetSemantics(TypeFrame types) {
            return PassingSemantics.ValueType;
        }

        public override HelixType GetMutationSupertype(TypeFrame types) {
            return PrimitiveType.Int;
        }

        public override HelixType GetSignatureSupertype(TypeFrame types) {
            return PrimitiveType.Int;
        }

        public override Option<ISyntaxTree> ToSyntax(TokenLocation loc, TypeFrame types) {
            return new IntLiteral(loc, this.Value);
        }       

        public override string ToString() => this.Value.ToString();
    }
}