using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree VoidLiteral() {
            var tok = this.Advance(TokenKind.VoidKeyword);

            return new VoidLiteral(tok.Location);
        }
    }
}

namespace Trophy.Features.Primitives {
    public record VoidLiteral : ISyntaxTree {
        public TokenLocation Location { get; }

        public VoidLiteral(TokenLocation loc) {
            this.Location = loc;
        }

        public Option<TrophyType> ToType(INamesRecorder names) => PrimitiveType.Void;

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            types.SetReturnType(this, PrimitiveType.Void);

            return this;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter statWriter) {
            return CExpression.IntLiteral(0);
        }
    }
}
