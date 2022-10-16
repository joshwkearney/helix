using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree IntLiteral() {
            var tok = this.Advance(TokenKind.IntLiteral);
            var num = int.Parse(tok.Value);

            return new IntLiteral(tok.Location, num);
        }
    }
}

namespace Trophy.Features.Primitives {
    public class IntLiteral : ISyntaxTree {
        public TokenLocation Location { get; }

        public int Value { get; }

        public IntLiteral(TokenLocation loc, int value) {
            this.Location = loc;
            this.Value = value;
        }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) => Option.None;

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            types.SetReturnType(this, PrimitiveType.Int);

            return this;
        }

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) => Option.None;

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter writer) {
            return CExpression.IntLiteral(this.Value);
        }
    }
}
