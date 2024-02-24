using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis;

namespace Helix.Parsing
{
    public partial class Parser {
        private IParseTree WordLiteral() {
            var tok = this.Advance(TokenKind.WordLiteral);
            var num = long.Parse(tok.Value);

            return new WordLiteral(tok.Location, num);
        }
    }
}

namespace Helix.Features.Primitives
{
    public record WordLiteral : IParseTree {
        public TokenLocation Location { get; }

        public long Value { get; }

        public IEnumerable<IParseTree> Children => Enumerable.Empty<IParseTree>();

        public bool IsPure => true;

        public WordLiteral(TokenLocation loc, long value) {
            this.Location = loc;
            this.Value = value;
        }

        public Option<HelixType> AsType(TypeFrame types) {
            return new SingularWordType(this.Value);
        }

        public IParseTree ToRValue(TypeFrame types) => this;

        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) {
            return ImperativeExpression.Word(this.Value);
        }
    }
}
