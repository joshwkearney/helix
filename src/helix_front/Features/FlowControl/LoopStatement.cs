using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.FlowControl;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Primitives;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseTree WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();

            var test = new IfSyntax(
                cond.Location,
                new UnaryParseSyntax(cond.Location, UnaryOperatorKind.Not, cond),
                new BreakContinueSyntax(cond.Location, true));

            // False loops will never run and true loops don't need a break test
            //if (cond is not Features.Primitives.BoolLiteral) {
            //    newBlock.Add(test);
            //}

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            this.isInLoop.Push(true);
            var body = this.TopExpression();
            this.isInLoop.Pop();

            var newBlock = new BlockSyntax(test, body);
            var loc = start.Location.Span(body.Location);
            var loop = new LoopStatement(loc, newBlock);

            return loop;
        }
    }
}

namespace Helix.Features.FlowControl {
    public record LoopStatement : IParseTree {
        private static int loopCounter = 0;

        private readonly IParseTree body;
        private readonly string name;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => new[] { this.body };

        public bool IsPure => false;

        public LoopStatement(TokenLocation location, IParseTree body, string name) {
            this.Location = location;
            this.body = body;
            this.name = name;
        }

        public LoopStatement(TokenLocation location, IParseTree body)
            : this(location, body, "$loop" + loopCounter++) { }
    }
}
