using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Arrays;
using Helix.Parsing;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        public IParseTree ArrayExpression(IParseTree start) {
            this.Advance(TokenKind.OpenBracket);

            if (this.Peek(TokenKind.CloseBracket)) {
                var end = this.Advance(TokenKind.CloseBracket);
                var loc = start.Location.Span(end.Location);

                return new ArrayTypeSyntax(loc, start);
            }
            else {
                var index = this.TopExpression();
                var end = this.Advance(TokenKind.CloseBracket);
                var loc = start.Location.Span(end.Location);

                return new ArrayIndexSyntax(loc, start, index);
            }            
        }
    }
}

namespace Helix.Features.Arrays {
    public record ArrayIndexSyntax : IParseTree {
        private readonly IParseTree target;
        private readonly IParseTree index;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => new[] { this.target, this.index };

        public bool IsPure { get; }

        public ArrayIndexSyntax(TokenLocation loc, IParseTree target, IParseTree index) {
            this.Location = loc;
            this.target = target;
            this.index = index;

            this.IsPure = this.target.IsPure && this.index.IsPure;
        }
    }
}