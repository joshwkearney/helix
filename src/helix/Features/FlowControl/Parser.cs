using Helix.Features.FlowControl;
using Helix.Features.FlowControl.ParseSyntax;
using Helix.Features.FlowControl.Syntax;
using Helix.Features.Primitives;
using Helix.Features.Primitives.ParseSyntax;
using Helix.Features.Primitives.Syntax;
using Helix.Features.Variables.ParseSyntax;
using Helix.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseSyntax Block() {
            var start = this.Advance(TokenKind.OpenBrace);
            var stats = new List<IParseSyntax>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                stats.Add(this.Statement());
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            return BlockParseSyntax.FromMany(loc, stats);
        }
        
        private IParseSyntax WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();
            var newBlock = new List<IParseSyntax>();

            var test = new IfParseSyntax {
                Location = cond.Location,
                Condition = new UnaryParseSyntax {
                    Location = cond.Location,
                    Operand = cond,
                    Operator = UnaryOperatorKind.Not
                },
                Affirmative = new LoopControlSyntax {
                    Location = cond.Location,
                    Kind = LoopControlKind.Break
                }
            };

            // False loops will never run and true loops don't need a break test
            if (cond is not Features.Primitives.Syntax.BoolLiteral) {
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
                Body = BlockParseSyntax.FromMany(loc, newBlock)
            };

            return loop;
        }
        
        private IParseSyntax BreakContinueStatement() {
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

            var end = this.Advance(TokenKind.Semicolon);
            
            return new LoopControlSyntax {
                Location = start.Location.Span(end.Location),
                Kind = kind
            };
        }
    
        private IParseSyntax ForStatement() {
            var startTok = this.Advance(TokenKind.ForKeyword);
            var id = this.Advance(TokenKind.Identifier);

            this.Advance(TokenKind.Assignment);
            var startIndex = this.TopExpression();

            var inclusive = true;
            if (!this.TryAdvance(TokenKind.ToKeyword)) {
                this.Advance(TokenKind.UntilKeyword);
                inclusive = false;
            }

            var endIndex = this.TopExpression();

            /*startIndex = new AsParseSyntax {
                Location = startIndex.Location,
                Operand = startIndex,
                TypeSyntax = new VariableAccessParseSyntax {
                    Location = startIndex.Location,
                    VariableName = "word"
                }
            };
            
            endIndex = new AsParseSyntax {
                Location = endIndex.Location,
                Operand = endIndex,
                TypeSyntax = new VariableAccessParseSyntax {
                    Location = endIndex.Location,
                    VariableName = "word"
                }
            };*/

            var counterName = id.Value;

            var counterDecl = new VariableParseStatement {
                Location = startTok.Location,
                VariableNames = [counterName],
                VariableTypes = [Option.None],
                Assignment = startIndex
            };

            var counterAccess = new VariableAccessParseSyntax {
                Location = startTok.Location,
                VariableName = counterName
            };

            var counterInc = new AssignmentParseStatement {
                Location = startTok.Location,
                Left = counterAccess,
                Right = new BinaryParseSyntax {
                    Location = startTok.Location,
                    Left = counterAccess,
                    Right = new WordLiteral {
                        Location = startTok.Location,
                        Value = 1
                    },
                    Operator = BinaryOperationKind.Add
                }
            };

            var totalBlock = new List<IParseSyntax> { counterDecl };
            var loopBlock = new List<IParseSyntax>();
            var loc = startTok.Location.Span(endIndex.Location);

            var test = new IfParseSyntax {
                Location = loc,
                Condition = new BinaryParseSyntax {
                    Location = loc,
                    Left = counterAccess,
                    Right = endIndex,
                    Operator = inclusive
                        ? BinaryOperationKind.GreaterThan
                        : BinaryOperationKind.GreaterThanOrEqualTo
                },
                Affirmative = new LoopControlSyntax {
                    Location = loc,
                    Kind = LoopControlKind.Break
                }
            };

            loopBlock.Add(test);

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            var body = this.TopExpression();
            loc = loc.Span(body.Location);

            loopBlock.Add(body);
            loopBlock.Add(counterInc);

            var loop = new LoopParseStatement {
                Location = loc,
                Body = BlockParseSyntax.FromMany(loc, loopBlock)
            };

            totalBlock.Add(loop);

            return BlockParseSyntax.FromMany(loc, totalBlock);
        }
        
        // This is like the if expression, but it doesn't need a "then" keyword,
        // and it requires brackets
        private IParseSyntax IfStatement() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();
            var affirm = this.Block();

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var neg = this.Block();
                var loc = start.Location.Span(neg.Location);

                return new IfParseSyntax {
                    Location = loc,
                    Condition = cond,
                    Affirmative = affirm,
                    Negative = Option.Some(neg)
                };
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfParseSyntax {
                    Location = loc,
                    Condition = cond,
                    Affirmative = affirm
                };
            }
        }
        
        private IParseSyntax IfExpression() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.TopExpression();

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var neg = this.TopExpression();
                var loc = start.Location.Span(neg.Location);

                return new IfParseSyntax {
                    Location = loc,
                    Condition = cond,
                    Affirmative = affirm,
                    Negative = Option.Some(neg)
                };
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfParseSyntax {
                    Location = loc,
                    Condition = cond,
                    Affirmative = affirm
                };
            }
        }
    }
}