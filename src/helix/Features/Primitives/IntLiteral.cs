using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Lifetimes;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree IntLiteral() {
            var tok = this.Advance(TokenKind.IntLiteral);
            var num = int.Parse(tok.Value);

            return new IntLiteral(tok.Location, num);
        }
    }
}

namespace Helix.Features.Primitives {
    public record IntLiteral : ISyntaxTree {
        public TokenLocation Location { get; }

        public int Value { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public IntLiteral(TokenLocation loc, int value) {
            this.Location = loc;
            this.Value = value;
        }

        public Option<HelixType> AsType(SyntaxFrame types) {
            return new SingularIntType(this.Value);
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            types.ReturnTypes[this] = new SingularIntType(this.Value);
            types.Lifetimes[this] = new LifetimeBundle();

            return this;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            return new CIntLiteral(this.Value);
        }
    }
}
