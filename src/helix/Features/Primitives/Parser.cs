using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Features.Unions;
using Helix.Features.Variables;
using Helix.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseSyntax UnaryExpression() {
            var hasOperator = this.Peek(TokenKind.Minus)
                           || this.Peek(TokenKind.Plus)
                           || this.Peek(TokenKind.Not)
                           || this.Peek(TokenKind.Ampersand);

            if (hasOperator) {
                var tokOp = this.Advance();
                var first = this.UnaryExpression();
                var loc = tokOp.Location.Span(first.Location);
                var op = UnaryOperatorKind.Not;

                if (tokOp.Kind == TokenKind.Plus) {
                    op = UnaryOperatorKind.Plus;
                }
                else if (tokOp.Kind == TokenKind.Minus) {
                    op = UnaryOperatorKind.Minus;
                }
                else if (tokOp.Kind == TokenKind.Ampersand) {
                    return new AddressOfParseSyntax {
                        Location = loc,
                        Operand = first
                    };
                }
                else if (tokOp.Kind != TokenKind.Not) {
                    throw new Exception("Unexpected unary operator");
                }

                return new UnaryParseSyntax {
                    Location = loc,
                    Operand = first,
                    Operator = op
                };
            }

            return this.SuffixExpression();
        }
        
        private IParseSyntax WordLiteral() {
            var tok = this.Advance(TokenKind.WordLiteral);
            var num = long.Parse(tok.Value);

            return new WordLiteral {
                Location = tok.Location,
                Value = num
            };
        }
        
        private IParseSyntax VoidLiteral() {
            var tok = this.Advance(TokenKind.VoidKeyword);

            return new VoidLiteral {
                Location = tok.Location
            };
        }
        
        private IParseSyntax BoolLiteral() {
            var start = this.Advance(TokenKind.BoolLiteral);
            var value = bool.Parse(start.Value);

            return new BoolLiteral {
                Location = start.Location,
                Value = value
            };
        }
        
        private IParseSyntax AsExpression() {
            var first = this.UnaryExpression();

            while (this.Peek(TokenKind.AsKeyword) || this.Peek(TokenKind.IsKeyword)) {
                if (this.TryAdvance(TokenKind.AsKeyword)) {
                    var target = this.TopExpression();
                    var loc = first.Location.Span(target.Location);

                    first = new AsParseSyntax {
                        Location = loc,
                        Operand = first,
                        TypeSyntax = target
                    };
                }
                else {
                    this.Advance(TokenKind.IsKeyword);
                    var nameTok = this.Advance(TokenKind.Identifier);

                    first = new IsParseSyntax() {
                        Location = first.Location.Span(nameTok.Location),
                        Operand = first,
                        MemberName = nameTok.Value
                    };
                }
            }

            return first;
        }
        
        private IParseSyntax OrExpression() {
            var first = this.XorExpression();

            while (this.TryAdvance(TokenKind.OrKeyword)) {
                var branching = this.TryAdvance(TokenKind.ElseKeyword);
                var second = this.XorExpression();
                var loc = first.Location.Span(second.Location);

                if (branching) {
                    first = new IfParseSyntax {
                        Location = loc,
                        Condition = first,
                        Affirmative = new BoolLiteral {
                            Location = loc,
                            Value = true
                        },
                        Negative = Option.Some(second)
                    };
                }
                else {
                    first = new BinaryParseSyntax {
                        Location = loc,
                        Left = first,
                        Right = second,
                        Operator = BinaryOperationKind.Or
                    };
                }
            }

            return first;
        }

        private IParseSyntax XorExpression() {
            var first = this.AndExpression();

            while (this.TryAdvance(TokenKind.XorKeyword)) {
                var second = this.AndExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinaryParseSyntax {
                    Location = loc,
                    Left = first,
                    Right = second,
                    Operator = BinaryOperationKind.Xor
                };
            }

            return first;
        }

        private IParseSyntax AndExpression() {
            var first = this.ComparisonExpression();

            while (this.TryAdvance(TokenKind.AndKeyword)) {
                var branching = this.TryAdvance(TokenKind.ThenKeyword);
                var second = this.XorExpression();
                var loc = first.Location.Span(second.Location);

                if (branching) {
                    first = new IfParseSyntax {
                        Location = loc,
                        Condition = new UnaryParseSyntax {
                            Location = loc,
                            Operand = first,
                            Operator = UnaryOperatorKind.Not
                        },
                        Affirmative = new BoolLiteral {
                            Location = loc,
                            Value = false
                        },
                        Negative = Option.Some(second)
                    };
                }
                else {
                    first = new BinaryParseSyntax {
                        Location = loc,
                        Left = first,
                        Right = second,
                        Operator = BinaryOperationKind.And
                    };
                }
            }

            return first;
        }

        private IParseSyntax ComparisonExpression() {
            var first = this.AddExpression();
            var comparators = new Dictionary<TokenKind, BinaryOperationKind>() {
                { TokenKind.Equals, BinaryOperationKind.EqualTo }, { TokenKind.NotEquals, BinaryOperationKind.NotEqualTo },
                { TokenKind.LessThan, BinaryOperationKind.LessThan }, { TokenKind.GreaterThan, BinaryOperationKind.GreaterThan },
                { TokenKind.LessThanOrEqualTo, BinaryOperationKind.LessThanOrEqualTo },
                { TokenKind.GreaterThanOrEqualTo, BinaryOperationKind.GreaterThanOrEqualTo }
            };

            while (true) {
                bool worked = false;

                foreach (var (tok, _) in comparators) {
                    worked |= this.Peek(tok);
                }

                if (!worked) {
                    break;
                }

                var op = comparators[this.Advance().Kind];
                var second = this.AddExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinaryParseSyntax {
                    Location = loc,
                    Left = first,
                    Right = second,
                    Operator = op
                };
            }

            return first;
        }

        private IParseSyntax AddExpression() {
            var first = this.MultiplyExpression();

            while (true) {
                if (!this.Peek(TokenKind.Plus) && !this.Peek(TokenKind.Minus)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.Plus ? BinaryOperationKind.Add : BinaryOperationKind.Subtract;
                var second = this.MultiplyExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinaryParseSyntax {
                    Location = loc,
                    Left = first,
                    Right = second,
                    Operator = op
                }; 
            }

            return first;
        }

        private IParseSyntax MultiplyExpression() {
            var first = this.PrefixExpression();

            while (true) {
                if (!this.Peek(TokenKind.Star) && !this.Peek(TokenKind.Modulo) && !this.Peek(TokenKind.Divide)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = BinaryOperationKind.Modulo;

                if (tok == TokenKind.Star) {
                    op = BinaryOperationKind.Multiply;
                }
                else if (tok == TokenKind.Divide) {
                    op = BinaryOperationKind.FloorDivide;
                }

                var second = this.PrefixExpression();
                var loc = first.Location.Span(second.Location);
                
                first = new BinaryParseSyntax {
                    Location = loc,
                    Left = first,
                    Right = second,
                    Operator = op
                };
            }

            return first;
        }
        
        private IParseSyntax NewExpression() {
            var start = this.Advance(TokenKind.NewKeyword).Location;
            var targetType = this.TopExpression();
            var loc = start.Span(targetType.Location);

            if (!this.TryAdvance(TokenKind.OpenBrace)) {
                return new NewParseSyntax {
                    Location = loc,
                    TypeSyntax = targetType
                };
            }

            var names = new List<string>();
            var values = new List<IParseSyntax>();

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

            return new NewParseSyntax {
                Location = loc,
                TypeSyntax = targetType,
                Names = names,
                Values = values
            };
        }
    }
}