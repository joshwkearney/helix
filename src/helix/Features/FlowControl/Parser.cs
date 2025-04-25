using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseSyntax WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();
            var newBlock = new List<IParseSyntax>();

            var test = new IfParse(
                cond.Location,
                new UnaryParseSyntax {
                    Location = cond.Location,
                    Operand = cond,
                    Operator = UnaryOperatorKind.Not
                },
                new LoopControlSyntax {
                    Location = cond.Location,
                    Kind = LoopControlKind.Break
                });

            // False loops will never run and true loops don't need a break test
            if (cond is not Features.Primitives.BoolLiteral) {
                newBlock.Add(test);
            }

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            this.isInLoop.Push(true);
            var body = this.TopExpression();
            this.isInLoop.Pop();

            newBlock.Add(body);

            var loc = start.Location.Span(body.Location);

            var loop = new LoopParseStatement {
                Location = loc,
                Body = BlockParse.FromMany(loc, newBlock)
            };

            return loop;
        }
        
        private IParseSyntax BreakStatement() {
            Token start;
            LoopControlKind kind;

            if (this.Peek(TokenKind.BreakKeyword)) {
                start = this.Advance(TokenKind.BreakKeyword);
                kind = LoopControlKind.Break;
            }
            else {
                start = this.Advance(TokenKind.ContinueKeyword);
                kind = LoopControlKind.Continue;
            }

            if (!this.isInLoop.Peek()) {
                throw new ParseException(
                    start.Location, 
                    "Invalid Statement", 
                    "Break and continue statements must only appear inside of loops");
            }

            return new LoopControlSyntax {
                Location = start.Location,
                Kind = kind
            };
        }
        
        private IParseSyntax IfExpression() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.TopExpression();

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var neg = this.TopExpression();
                var loc = start.Location.Span(neg.Location);

                return new IfParse(loc, cond, affirm, neg);
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfParse(loc, cond, affirm);
            }
        }
    }
}