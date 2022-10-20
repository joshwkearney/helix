using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

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
    public record IntLiteral : ISyntaxTree {
        public TokenLocation Location { get; }

        public int Value { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public IntLiteral(TokenLocation loc, int value) {
            this.Location = loc;
            this.Value = value;
        }

        public Option<TrophyType> AsType(SyntaxFrame types) {
            return new SingularIntType(this.Value);
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            types.ReturnTypes[this] = new SingularIntType(this.Value);

            return this;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            return new CIntLiteral(this.Value);
        }
    }
}
