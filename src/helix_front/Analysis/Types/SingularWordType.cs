using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Features.Primitives;
using Helix.Parsing;

namespace Helix.Analysis.Types {
    public record SingularWordType : HelixType {
        public long Value { get; }

        public SingularWordType(long value) {
            this.Value = value;
        }

        public override PassingSemantics GetSemantics(TypeFrame types) {
            return PassingSemantics.ValueType;
        }

        public override HelixType GetMutationSupertype(TypeFrame types) {
            return PrimitiveType.Word;
        }

        public override HelixType GetSignatureSupertype(TypeFrame types) {
            return PrimitiveType.Word;
        }

        public override Option<ISyntaxTree> ToSyntax(TokenLocation loc, TypeFrame types) {
            return new WordLiteral(loc, this.Value);
        }       

        public override string ToString() => this.Value.ToString();
    }
}