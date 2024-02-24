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
        private IParseTree BoolLiteral() {
            var start = this.Advance(TokenKind.BoolLiteral);
            var value = bool.Parse(start.Value);

            return new BoolLiteral(start.Location, value);
        }
    }
}

namespace Helix.Features.Primitives
{
    public record BoolLiteral : IParseTree {
        public TokenLocation Location { get; }

        public bool Value { get; }

        public IEnumerable<IParseTree> Children => Enumerable.Empty<IParseTree>();

        public bool IsPure => true;

        public BoolLiteral(TokenLocation loc, bool value) {
            this.Location = loc;
            this.Value = value;
        }

        public Option<HelixType> AsType(TypeFrame types) {
            return new SingularBoolType(this.Value);
        }

        public IParseTree ToRValue(TypeFrame types) => this;

        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) {
            return ImperativeExpression.Bool(this.Value);
        }
    }
}
