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
        private IParseTree VoidLiteral() {
            var tok = this.Advance(TokenKind.VoidKeyword);

            return new VoidLiteral(tok.Location);
        }
    }
}

namespace Helix.Features.Primitives
{
    public record VoidLiteral : IParseTree {
        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => Enumerable.Empty<IParseTree>();

        public bool IsPure => true;

        public VoidLiteral(TokenLocation loc) {
            this.Location = loc;
        }

        public IParseTree ToRValue(TypeFrame types) => this;

        public Option<HelixType> AsType(TypeFrame types) => PrimitiveType.Void;

        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) {
            return ImperativeExpression.Void;
        }
    }
}
