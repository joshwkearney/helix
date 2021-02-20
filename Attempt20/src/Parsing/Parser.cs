using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.Features.Containers;
using Attempt20.Features.Containers.Arrays;
using Attempt20.Features.FlowControl;
using Attempt20.Features.Functions;
using Attempt20.Features.Primitives;
using Attempt20.Features.Variables;

namespace Attempt20.Parsing {
    public class Parser {
        private int pos = 0;
        private readonly IReadOnlyList<IToken> tokens;

        public Parser(IReadOnlyList<IToken> tokens) {
            this.tokens = tokens;
        }

        public IReadOnlyList<IParsedDeclaration> Parse() {
            var list = new List<IParsedDeclaration>();

            while (pos < tokens.Count) {
                list.Add(this.Declaration());
            }

            return list;
        }

        private bool Peek(TokenKind kind) {
            return this.Peek(kind, 1);
        }

        private bool Peek(TokenKind kind, int count) {
            if (this.pos + count - 1 >= this.tokens.Count) {
                return false;
            }

            return this.tokens[this.pos + count - 1].Kind == kind;
        }

        private bool TryAdvance(TokenKind kind) {
            if (this.Peek(kind)) {
                this.Advance(kind);
                return true;
            }

            return false;
        }

        private IToken Advance() {
            if (this.pos >= this.tokens.Count) {
                throw ParsingErrors.EndOfFile(this.tokens.Last().Location);
            }

            return this.tokens[this.pos++];
        }

        private IToken Advance(TokenKind kind) {
            var tok = this.Advance();

            if (tok.Kind != kind) {
                throw ParsingErrors.UnexpectedToken(kind, tok);
            }

            return tok;
        }

        private T Advance<T>() {
            var tok = this.Advance();

            if (!(tok is Token<T> ttok)) {
                throw ParsingErrors.UnexpectedToken(tok);
            }

            return ttok.Value;
        }

        /** Type Parsing **/
        private TrophyType VarTypeExpression() {
            this.Advance(TokenKind.VarKeyword);
            var inner = this.TypeExpression();

            return new VariableType(inner);
        }

        private TrophyType TypeExpression() {
            if (this.Peek(TokenKind.VarKeyword)) {
                return this.VarTypeExpression();
            }

            return this.ArrayTypeExpression();
        }

        private TrophyType ArrayTypeExpression() {
            var start = this.TypeAtom();

            while (this.Peek(TokenKind.OpenBracket)) {
                this.Advance(TokenKind.OpenBracket);

                if (this.Peek(TokenKind.IntLiteral)) {
                    var size = this.Advance<int>();
                    this.Advance(TokenKind.CloseBracket);
                    start = new FixedArrayType(start, size);
                }
                else {
                    this.Advance(TokenKind.CloseBracket);
                    start = new ArrayType(start);
                }
            }

            return start;
        }

        private TrophyType TypeAtom() {
            if (this.TryAdvance(TokenKind.IntKeyword)) {
                return TrophyType.Integer;
            }
            else if (this.TryAdvance(TokenKind.VoidKeyword)) {
                return TrophyType.Void;
            }
            else if (this.TryAdvance(TokenKind.BoolKeyword)) {
                return TrophyType.Boolean;
            }
            else {
                return new NamedType(new IdentifierPath(this.Advance<string>()));
            }
        }

        /** Lifetime Parsing **/
        private string Lifetime() {
            if (this.TryAdvance(TokenKind.StackKeyword)) {
                return "stack";
            }
            else if (this.TryAdvance(TokenKind.HeapKeyword)) {
                return "heap";
            }
            else {
                return this.Advance<string>();
            }
        }

        /** Declaration Parsing **/
        private FunctionSignature FunctionSignature() {
            this.Advance(TokenKind.FunctionKeyword);

            var returnType = this.TypeExpression();
            var funcName = this.Advance<string>();

            this.Advance(TokenKind.OpenParenthesis);

            var pars = ImmutableList<FunctionParameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var parType = this.TypeExpression();
                var parName = this.Advance<string>();

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new FunctionParameter(parName, parType));
            }

            this.Advance(TokenKind.CloseParenthesis);

            var sig = new FunctionSignature(funcName, returnType, pars);

            return sig;
        }

        private IParsedDeclaration FunctionDeclaration() {
            var start = this.tokens[this.pos];
            var sig = this.FunctionSignature();
            var end = this.Advance(TokenKind.YieldSign);

            var body = this.TopExpression();
            var loc = start.Location.Span(end.Location);

            this.Advance(TokenKind.Semicolon);

            return new FunctionParseDeclaration() {
                Location = loc,
                Body = body,
                Signature = sig
            };
        }

        private IParsedDeclaration AggregateDeclaration() {
            IToken start;
            if (this.Peek(TokenKind.StructKeyword)) {
                start = this.Advance(TokenKind.StructKeyword);
            }
            else {
                start = this.Advance(TokenKind.UnionKeyword);
            }

            var name = this.Advance<string>();
            var mems = new List<StructMember>();
            var decls = new List<IParsedDeclaration>();

            this.Advance(TokenKind.OpenBrace);

            while (!this.Peek(TokenKind.FunctionKeyword) && !this.Peek(TokenKind.StructKeyword) && !this.Peek(TokenKind.CloseBrace)) {
                var memType = this.TypeExpression();
                var memName = this.Advance<string>();

                this.Advance(TokenKind.Semicolon);
                mems.Add(new StructMember(memName, memType));
            }

            while (!this.Peek(TokenKind.CloseBrace)) {
                decls.Add(this.Declaration());
            }

            this.Advance(TokenKind.CloseBrace);
            var last = this.Advance(TokenKind.Semicolon);

            return new AggregateParsedDeclaration() {
                Location = start.Location.Span(last.Location),
                Signature = new StructSignature(name, mems),
                Declarations = decls,
                Kind = start.Kind == TokenKind.StructKeyword ? AggregateKind.Struct : AggregateKind.Union
            };
        }

        private IParsedDeclaration Declaration() {
            if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionDeclaration();
            }
            else if (this.Peek(TokenKind.StructKeyword) || this.Peek(TokenKind.UnionKeyword)) {
                return this.AggregateDeclaration();
            }

            throw ParsingErrors.UnexpectedToken(this.Advance());
        }

        /** Expression Parsing **/
        private IParsedSyntax TopExpression() => this.AsExpression();

        private IParsedSyntax AsExpression() {
            var first = this.OrExpression();

            while (this.TryAdvance(TokenKind.AsKeyword)) {
                var target = this.TypeExpression();

                first = new AsParsedSyntax() {
                    Location = first.Location.Span(this.tokens[this.pos - 1].Location),
                    Argument = first,
                    TargetType = target
                };
            }

            return first;
        }

        private IParsedSyntax OrExpression() {
            var first = this.XorExpression();

            while (this.TryAdvance(TokenKind.OrKeyword)) {
                var second = this.XorExpression();

                first = new BinaryParseSyntax() {
                    Location = first.Location.Span(second.Location),
                    Operation = BinaryOperation.Or,
                    LeftArgument = first,
                    RightArgument = second
                };
            }

            return first;
        }

        private IParsedSyntax XorExpression() {
            var first = this.ComparisonExpression();

            while (this.TryAdvance(TokenKind.XorKeyword)) {
                var second = this.ComparisonExpression();

                first = new BinaryParseSyntax() {
                    Location = first.Location.Span(second.Location),
                    Operation = BinaryOperation.Xor,
                    LeftArgument = first,
                    RightArgument = second
                };
            }

            return first;
        }

        private IParsedSyntax ComparisonExpression() {
            var first = this.AndExpression();
            var comparators = new Dictionary<TokenKind, BinaryOperation>() {
                { TokenKind.EqualSign, BinaryOperation.EqualTo }, { TokenKind.NotEqualSign, BinaryOperation.NotEqualTo },
                { TokenKind.LessThanSign, BinaryOperation.LessThan }, { TokenKind.GreaterThanSign, BinaryOperation.GreaterThan },
                { TokenKind.LessThanOrEqualToSign, BinaryOperation.LessThanOrEqualTo },
                { TokenKind.GreaterThanOrEqualToSign, BinaryOperation.GreaterThanOrEqualTo }
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
                var second = this.AndExpression();

                first = new BinaryParseSyntax() {
                    Location = first.Location.Span(second.Location),
                    Operation = op,
                    LeftArgument = first,
                    RightArgument = second
                };
            }

            return first;
        }

        private IParsedSyntax AndExpression() {
            var first = this.AddExpression();

            while (this.TryAdvance(TokenKind.AndKeyword)) {
                var second = this.AddExpression();

                first = new BinaryParseSyntax() {
                    Location = first.Location.Span(second.Location),
                    Operation = BinaryOperation.And,
                    LeftArgument = first,
                    RightArgument = second
                };
            }

            return first;
        }

        private IParsedSyntax AddExpression() {
            var first = this.MultiplyExpression();

            while (true) {
                if (!this.Peek(TokenKind.AddSign) && !this.Peek(TokenKind.SubtractSign)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.AddSign ? BinaryOperation.Add : BinaryOperation.Subtract;
                var second = this.MultiplyExpression();

                first = new BinaryParseSyntax() {
                    Location = first.Location.Span(second.Location),
                    Operation = op,
                    LeftArgument = first,
                    RightArgument = second
                };
            }

            return first;
        }

        private IParsedSyntax MultiplyExpression() {
            var first = this.SuffixExpression();

            while (true) {
                if (!this.Peek(TokenKind.MultiplySign)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.MultiplySign ? BinaryOperation.Multiply : BinaryOperation.Multiply;
                var second = this.SuffixExpression();

                first = new BinaryParseSyntax() {
                    Location = first.Location.Span(second.Location),
                    Operation = op,
                    LeftArgument = first,
                    RightArgument = second
                };
            }

            return first;
        }

        private IParsedSyntax SuffixExpression() {
            var first = this.Atom();

            while (this.Peek(TokenKind.OpenParenthesis) || this.Peek(TokenKind.OpenBracket) || this.Peek(TokenKind.LiteralSign) || this.Peek(TokenKind.Dot)) {
                if (this.Peek(TokenKind.OpenParenthesis)) {
                    first = this.InvokeExpression(first);
                }
                else if (this.Peek(TokenKind.OpenBracket)) {
                    first = this.ArrayIndexExpression(first);
                }
                else if (this.Peek(TokenKind.Dot)) {
                    first = this.MemberAccess(first);
                }
                else {
                    first = this.LiteralArrayIndexExpression(first);
                }
            }

            return first;
        }

        private IParsedSyntax MemberAccess(IParsedSyntax first) {
            this.Advance(TokenKind.Dot);
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);

            if (this.TryAdvance(TokenKind.OpenParenthesis)) {
                var args = new List<IParsedSyntax>();

                while (!this.Peek(TokenKind.CloseParenthesis)) {
                    args.Add(this.TopExpression());

                    if (!this.TryAdvance(TokenKind.Comma)) {
                        break;
                    }
                }

                var last = this.Advance(TokenKind.CloseParenthesis);

                return new MemberInvokeParsedSyntax() {
                    Location = first.Location.Span(last.Location),
                    Target = first,
                    Arguments = args,
                    MemberName = tok.Value
                };
            }
            else {
                return new MemberAccessParsedSyntax {
                    Location = first.Location.Span(tok.Location),
                    MemberName = tok.Value,
                    Target = first
                };
            }
        }

        private IParsedSyntax InvokeExpression(IParsedSyntax first) {
            this.Advance(TokenKind.OpenParenthesis);

            var args = new List<IParsedSyntax>();

            while (!this.Peek(TokenKind.CloseParenthesis)) {
                args.Add(this.TopExpression());

                if (!this.TryAdvance(TokenKind.Comma)) {
                    break;
                }
            }

            var last = this.Advance(TokenKind.CloseParenthesis);

            return new FunctionInvokeParsedSyntax() {
                Location = first.Location.Span(last.Location),
                Target = first,
                Arguments = args
            };
        }

        private IParsedSyntax ArrayIndexExpression(IParsedSyntax first) {
            this.Advance(TokenKind.OpenBracket);
            var index = this.TopExpression();
            var last = this.Advance(TokenKind.CloseBracket);

            return new ArrayAccessParsedSyntax() {
                Location = first.Location.Span(last.Location),
                Index = index,
                Target = first,
                AccessKind = ArrayAccessKind.ValueAccess
            };
        }

        private IParsedSyntax LiteralArrayIndexExpression(IParsedSyntax first) {
            this.Advance(TokenKind.LiteralSign);
            this.Advance(TokenKind.OpenBracket);

            if (this.TryAdvance(TokenKind.Colon)) {
                var index2 = this.TopExpression();
                var last = this.Advance(TokenKind.CloseBracket);

                return new ArraySliceParsedSyntax() {
                    Location = first.Location.Span(last.Location),
                    StartIndex = Option.None<IParsedSyntax>(),
                    EndIndex = Option.Some(index2),
                    Target = first
                };
            }
            else {
                var index = this.TopExpression();

                if (this.TryAdvance(TokenKind.Colon)) {
                    if (this.Peek(TokenKind.CloseBracket)) {
                        var last = this.Advance(TokenKind.CloseBracket);

                        return new ArraySliceParsedSyntax() {
                            Location = first.Location.Span(last.Location),
                            StartIndex = Option.Some(index),
                            EndIndex = Option.None<IParsedSyntax>(),
                            Target = first
                        };
                    }
                    else {
                        var index2 = this.TopExpression();
                        var last = this.Advance(TokenKind.CloseBracket);

                        return new ArraySliceParsedSyntax() {
                            Location = first.Location.Span(last.Location),
                            StartIndex = Option.Some(index),
                            EndIndex = Option.Some(index2),
                            Target = first
                        };
                    }
                }
                else {
                    var last = this.Advance(TokenKind.CloseBracket);

                    return new ArrayAccessParsedSyntax() {
                        Location = first.Location.Span(last.Location),
                        Index = index,
                        Target = first,
                        AccessKind = ArrayAccessKind.LiteralAccess
                    };
                }
            }
        }

        private IParsedSyntax Atom() {
            if (this.Peek(TokenKind.Identifier)) {
                return this.VariableAccess();
            }
            else if (this.Peek(TokenKind.LiteralSign)) {
                return this.LiteralVariableAccess();
            }
            else if (this.Peek(TokenKind.IntLiteral)) {
                return this.IntLiteral();
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.Block();
            }
            else if (this.Peek(TokenKind.VoidKeyword)) {
                return this.VoidLiteral();
            }
            else if (this.Peek(TokenKind.OpenBracket)) {
                return this.ArrayLiteral();
            }
            else if (this.Peek(TokenKind.OpenParenthesis)) {
                return this.ParenExpression();
            }
            else if (this.Peek(TokenKind.BoolLiteral)) {
                return this.BoolLiteral();
            }
            else if (this.Peek(TokenKind.IfKeyword)) {
                return this.IfExpression();
            }
            else if (this.Peek(TokenKind.FromKeyword)) {
                return this.FromExpression();
            }
            else if (this.Peek(TokenKind.RegionKeyword)) {
                return this.RegionExpression();
            }
            else if (this.Peek(TokenKind.NewKeyword)) {
                return this.NewExpression();
            }
            else {
                var next = this.Advance();

                throw ParsingErrors.UnexpectedToken(next);
            }
        }

        private IParsedSyntax LiteralVariableAccess() {
            var start = this.Advance(TokenKind.LiteralSign);
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);

            return new VariableAccessParseSyntax() {
                Location = start.Location.Span(tok.Location),
                VariableName = tok.Value,
                AccessKind = VariableAccessKind.LiteralAccess
            };
        }

        private IParsedSyntax VariableAccess() {
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);

            return new VariableAccessParseSyntax() {
                Location = tok.Location,
                VariableName = tok.Value,
                AccessKind = VariableAccessKind.ValueAccess
            };
        }

        private IParsedSyntax IntLiteral() {
            var tok = (Token<int>)this.Advance(TokenKind.IntLiteral);

            return new IntLiteralSyntax() {
                Location = tok.Location,
                Value = tok.Value
            };
        }

        private IParsedSyntax BoolLiteral() {
            var start = (Token<bool>)this.Advance(TokenKind.BoolLiteral);

            return new BoolLiteralSyntax() {
                Location = start.Location,
                Value = start.Value
            };
        }

        private IParsedSyntax VoidLiteral() {
            var tok = this.Advance(TokenKind.VoidKeyword);

            return new VoidLiteralSyntax() {
                Location = tok.Location
            };
        }

        private IParsedSyntax ArrayLiteral() {
            var start = this.Advance(TokenKind.OpenBracket);
            var args = new List<IParsedSyntax>();

            while (!this.Peek(TokenKind.CloseBracket)) {
                args.Add(this.TopExpression());

                if (!this.TryAdvance(TokenKind.Comma)) {
                    break;
                }
            }

            var end = this.Advance(TokenKind.CloseBracket);

            return new ArrayParsedLiteral() {
                Arguments = args,
                Location = start.Location.Span(end.Location)
            };
        }

        private IParsedSyntax ParenExpression() {
            this.Advance(TokenKind.OpenParenthesis);
            var result = this.TopExpression();
            this.Advance(TokenKind.CloseParenthesis);

            return result;
        }

        private IParsedSyntax Block() {
            var start = this.Advance(TokenKind.OpenBrace);
            var list = new List<IParsedSyntax>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                list.Add(this.Statement());
            }

            var end = this.Advance(TokenKind.CloseBrace);

            return new BlockParseSyntax() {
                Location = start.Location.Span(end.Location),
                Statements = list
            };
        }

        private IParsedSyntax Statement() {
            IParsedSyntax result;

            if (this.Peek(TokenKind.WhileKeyword)) {
                result = this.WhileStatement();
            }
            else if (this.Peek(TokenKind.VarKeyword)) {
                result = this.VariableDeclarationStatement();
            }
            else {
                result = this.StoreStatement();
            }

            this.Advance(TokenKind.Semicolon);

            return result;
        }

        private IParsedSyntax WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.DoKeyword);
            var body = this.TopExpression();

            return new WhileParsedSyntax() {
                Location = start.Location.Span(body.Location),
                Body = body,
                Condition = cond
            };
        }

        private IParsedSyntax VariableDeclarationStatement() {
            var tok = this.Advance(TokenKind.VarKeyword);
            var name = this.Advance<string>();

            this.Advance(TokenKind.LeftArrow);

            var value = this.TopExpression();

            return new LetParseSyntax() {
                Location = tok.Location.Span(value.Location),
                VariableName = name,
                AssignExpression = value
            };
        }

        private IParsedSyntax StoreStatement() {
            var start = this.TopExpression();

            if (this.TryAdvance(TokenKind.LeftArrow)) {
                var assign = this.TopExpression();

                start = new StoreParsedSyntax() {
                    Target = start,
                    AssignExpression = assign,
                    Location = start.Location.Span(assign.Location)
                };
            }

            return start;
        }

        private IParsedSyntax IfExpression() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();

            if (this.TryAdvance(TokenKind.ThenKeyword)) {
                var affirm = this.TopExpression();

                this.Advance(TokenKind.ElseKeyword);
                var neg = this.TopExpression();

                return new IfParsedSyntax() {
                    Condition = cond,
                    TrueBranch = affirm,
                    FalseBranch = Option.Some(neg),
                    Location = start.Location.Span(neg.Location)
                };
            }
            else {
                this.Advance(TokenKind.DoKeyword);
                var affirm = this.TopExpression();

                return new IfParsedSyntax() {
                    Condition = cond,
                    TrueBranch = affirm,
                    FalseBranch = Option.None<IParsedSyntax>(),
                    Location = start.Location.Span(affirm.Location)
                };
            }
        }

        private IParsedSyntax FromExpression() {
            var start = this.Advance(TokenKind.FromKeyword);
            var region = this.Lifetime();

            this.Advance(TokenKind.DoKeyword);
            var expr = this.TopExpression();

            return new FromDoParsedSyntax() {
                Location = start.Location.Span(expr.Location),
                RegionName = region,
                Target = expr
            };
        }

        private IParsedSyntax RegionExpression() {
            var start = this.Advance(TokenKind.RegionKeyword);

            if (this.Peek(TokenKind.OpenBrace)) {
                var body = this.Block();

                return new RegionBlockParsedSyntax() {
                    Location = start.Location.Span(body.Location),
                    Body = body,
                    RegionName = Option.None<string>()
                };
            }
            else {
                var name = this.Advance<string>();
                var body = this.Block();

                return new RegionBlockParsedSyntax() {
                    Location = start.Location.Span(body.Location),
                    Body = body,
                    RegionName = Option.Some(name)
                };
            }
        }

        private IParsedSyntax NewExpression() {
            var start = this.Advance(TokenKind.NewKeyword);
            var name = this.TypeExpression();
            var mems = new List<StructArgument<IParsedSyntax>>();

            if (!this.TryAdvance(TokenKind.OpenBrace)) {
                return new NewParsedSyntax() {
                    Location = start.Location,
                    Target = name,
                    Arguments = mems
                };
            }

            while (!this.Peek(TokenKind.CloseBrace)) {
                var memName = this.Advance<string>();
                this.Advance(TokenKind.AssignmentSign);

                var memValue = this.TopExpression();

                mems.Add(new StructArgument<IParsedSyntax>() {
                    MemberName = memName,
                    MemberValue = memValue
                });

                if (!this.Peek(TokenKind.CloseBrace)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            var end = this.Advance(TokenKind.CloseBrace);

            return new NewParsedSyntax() {
                Location = start.Location.Span(end.Location),
                Target = name,
                Arguments = mems
            };
        }
    }
}