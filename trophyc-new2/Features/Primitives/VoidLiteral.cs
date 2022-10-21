using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

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

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public VoidLiteral(TokenLocation loc) {
            this.Location = loc;
        }

        public Option<TrophyType> AsType(SyntaxFrame types) => PrimitiveType.Void;

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            types.ReturnTypes[this] = PrimitiveType.Void;

            return this;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            return new CIntLiteral(0);
        }
    }
}
