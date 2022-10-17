using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax VoidLiteral() {
            var tok = this.Advance(TokenKind.VoidKeyword);

            return new VoidLiteral(tok.Location);
        }
    }
}

namespace Trophy.Features.Primitives {
    public record VoidLiteral : ISyntax {
        public TokenLocation Location { get; }

        public VoidLiteral(TokenLocation loc) {
            this.Location = loc;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) => PrimitiveType.Void;

        public ISyntax CheckTypes(ITypesRecorder types) {
            types.SetReturnType(this, PrimitiveType.Void);

            return this;
        }

        public ISyntax ToRValue(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            return new CIntLiteral(0);
        }
    }
}
