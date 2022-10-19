using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax BoolLiteral() {
            var start = this.Advance(TokenKind.BoolLiteral);
            var value = bool.Parse(start.Value);

            return new BoolLiteral(start.Location, value);
        }
    }
}

namespace Trophy.Features.Primitives {
    public record BoolLiteral : ISyntax {
        public TokenLocation Location { get; }

        public bool Value { get; }

        public BoolLiteral(TokenLocation loc, bool value) {
            this.Location = loc;
            this.Value = value;
        }

        public Option<TrophyType> AsType(ITypesRecorder names) {
            return new SingularBoolType(this.Value);
        }

        public ISyntax CheckTypes(ITypesRecorder types) {
            types.SetReturnType(this, new SingularBoolType(this.Value));

            return this;
        }

        public ISyntax ToRValue(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            return new CIntLiteral(this.Value ? 1 : 0);
        }
    }
}
