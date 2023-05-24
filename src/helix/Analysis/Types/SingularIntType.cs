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
        
        public override HelixType GetNaturalSupertype(ITypedFrame types) {
            return PrimitiveType.Int;
        }

        public override string ToString() => this.Value.ToString();
    }
}