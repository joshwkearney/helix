using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Features.Aggregates;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseTree NewExpression() {
            var start = this.Advance(TokenKind.NewKeyword).Location;
            var targetType = this.TopExpression();
            var loc = start.Span(targetType.Location);

            if (!this.TryAdvance(TokenKind.OpenBrace)) {
                return new NewSyntax(loc, targetType);
            }

            var names = new List<string>();
            var values = new List<IParseTree>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                string name = null;

                if (this.Peek(TokenKind.Identifier)) {
                    name = this.Advance(TokenKind.Identifier).Value;
                    this.Advance(TokenKind.Assignment);
                }

                var value = this.TopExpression();

                names.Add(name);
                values.Add(value);

                if (!this.Peek(TokenKind.CloseBrace)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            var end = this.Advance(TokenKind.CloseBrace);
            loc = start.Span(end.Location);

            return new NewSyntax(loc, targetType, names, values);
        }
    }
}

namespace Helix.Features.Primitives {
    public class NewSyntax : IParseTree {
        private readonly IParseTree type;
        private readonly IReadOnlyList<string> names;
        private readonly IReadOnlyList<IParseTree> values;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => this.values.Prepend(this.type);

        public bool IsPure { get; }

        public NewSyntax(TokenLocation loc, IParseTree type,
            IReadOnlyList<string> names, IReadOnlyList<IParseTree> values) {

            this.Location = loc;
            this.type = type;
            this.names = names;
            this.values = values;

            this.IsPure = type.IsPure && values.All(x => x.IsPure);
        }

        public NewSyntax(TokenLocation loc, IParseTree type) {
            this.Location = loc;
            this.type = type;
            this.names = Array.Empty<string>();
            this.values = Array.Empty<IParseTree>();

            this.IsPure = type.IsPure;
        }
    }
}