using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Features.Primitives.Syntax;
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

        public override HelixType GetSignature(TypeFrame types) {
            return PrimitiveType.Word;
        }

        public override bool IsWord(TypeFrame types) => true;

        public override Option<ISyntax> ToSyntax(TokenLocation loc, TypeFrame types) {
            return new WordLiteral {
                Location = loc,
                Value = this.Value
            };
        }       

        public override string ToString() => this.Value.ToString();
    }
}