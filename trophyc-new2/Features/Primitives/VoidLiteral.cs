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
    public class VoidLiteral : ISyntaxTree {
        public TokenLocation Location { get; }

        public VoidLiteral(TokenLocation loc) {
            this.Location = loc;
        }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) {
            return PrimitiveType.Void;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            types.SetReturnType(this, PrimitiveType.Void);

            return this;
        }

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) => Option.None;

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter statWriter) {
            return CExpression.IntLiteral(0);
        }
    }
}
