using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree WordLiteral() {
            var tok = this.Advance(TokenKind.WordLiteral);
            var num = long.Parse(tok.Value);

            return new WordLiteral(tok.Location, num);
        }
    }
}

namespace Helix.Features.Primitives {
    public record WordLiteral : ISyntaxTree {
        public TokenLocation Location { get; }

        public long Value { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public WordLiteral(TokenLocation loc, long value) {
            this.Location = loc;
            this.Value = value;
        }

        public Option<HelixType> AsType(TypeFrame types) {
            return new SingularWordType(this.Value);
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            SyntaxTagBuilder.AtFrame(types)
                .WithReturnType(new SingularWordType(this.Value))
                .BuildFor(this);

            return this;
        }

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CIntLiteral(this.Value);
        }
    }
}
