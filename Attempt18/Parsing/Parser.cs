using Attempt19.Features;
using Attempt19.Features.Containers;
using Attempt19.Features.Containers.Arrays;
using Attempt19.Features.Containers.Structs;
using Attempt19.Features.FlowControl;
using Attempt19.Features.Functions;
using Attempt19.Features.Primitives;
using Attempt19.Features.Variables;
using Attempt19.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt19.Parsing {
    public class Parser {
        private int pos = 0;
        private readonly IReadOnlyList<IToken> tokens;

        public Parser(IReadOnlyList<IToken> tokens) {
            this.tokens = tokens;
        }

        public ISyntax[] Parse() {
            var list = new List<ISyntax>();

            while (this.pos < this.tokens.Count) {
                list.Add(this.Declaration());
            }

            return list.ToArray();
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
                //throw ParsingErrors.EndOfFile(this.tokens.Last().Location);
                throw new Exception();
            }

            return this.tokens[this.pos++];
        }

        private IToken Advance(TokenKind kind) {
            var tok = this.Advance();

            if (tok.Kind != kind) {
                //throw ParsingErrors.UnexpectedToken(kind, tok);
                throw new Exception();
            }

            return tok;
        }

        private T Advance<T>() {
            var tok = this.Advance();

            if (!(tok is Token<T> ttok)) {
                //throw ParsingErrors.UnexpectedToken(tok);
                throw new Exception();
            }

            return ttok.Value;
        }
       
        private LanguageType VarTypeExpression() {
            this.Advance(TokenKind.VarKeyword);
            var inner = this.TypeExpression();

            return new VariableType(inner);
        }

        private LanguageType TypeExpression() {
            if (this.Peek(TokenKind.VarKeyword)) {
                return this.VarTypeExpression();
            }

            return this.ArrayTypeExpression();
        }

        private LanguageType ArrayTypeExpression() {
            var start = this.TypeAtom();

            while (this.Peek(TokenKind.OpenBracket)) {
                if (!this.Peek(TokenKind.CloseBracket, 2)) {
                    break;
                }

                this.Advance(TokenKind.OpenBracket);
                this.Advance(TokenKind.CloseBracket);

                start = new ArrayType(start);
            }

            return start;
        }

        private LanguageType TypeAtom() {
            if (this.TryAdvance(TokenKind.IntKeyword)) {
                return IntType.Instance;
            }
            else if (this.TryAdvance(TokenKind.VoidKeyword)) {
                return VoidType.Instance;
            }
            else if (this.TryAdvance(TokenKind.BoolKeyword)) {
                return BoolType.Instance;
            }
            else {
                var path = new IdentifierPath(this.Advance<string>());
                return new UnresolvedType(path);
            }
        }

        private FunctionSignature FunctionSignature() {
            this.Advance(TokenKind.FunctionKeyword);

            string funcName = this.Advance<string>();
            this.Advance(TokenKind.OpenParenthesis);

            var pars = new List<Parameter>();
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var parType = this.TypeExpression();
                var parName = this.Advance<string>();

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars.Add(new Parameter() {
                    Name = parName,
                    Type = parType
                });
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.YieldSign);

            var returnType = this.TypeExpression();
            var sig = new FunctionSignature() {
                Name = funcName,
                Parameters = pars.ToArray(),
                ReturnType = returnType
            };
            
            return sig;
        }

        private ISyntax FunctionDeclaration() {
            var sig = this.FunctionSignature();
            this.Advance(TokenKind.Colon);

            var body = this.TopExpression();

            this.Advance(TokenKind.Semicolon);

            return new FunctionDeclaration() {
                FunctionBody = body,
                Signature = sig
            };
        }

        private ISyntax StructBody(TokenLocation start, string name) {

            var mems = new List<Parameter>();
            var decls = new List<ISyntax>();

            this.Advance(TokenKind.OpenBrace);

            while (!this.Peek(TokenKind.CloseBrace)) {
                if (this.Peek(TokenKind.FunctionKeyword)
                    || this.Peek(TokenKind.StructKeyword)
                    || this.Peek(TokenKind.ClassKeyword)) {

                    decls.Add(this.Declaration());
                }
                else {
                    var memType = this.TypeExpression();
                    var memName = this.Advance<string>();

                    this.Advance(TokenKind.Semicolon);

                    mems.Add(new Parameter() {
                        Name = memName,
                        Type = memType
                    });
                }
            }

            var last = this.Advance(TokenKind.CloseBrace);
            var loc = start.Span(last.Location);

            return new StructDeclaration() {
                Name = name,
                Members = mems.ToArray(),
                Declarations = decls.ToArray()
            };
        }

        private ISyntax StructDeclaration() {
            var first = this.Advance(TokenKind.StructKeyword);

            var name = this.Advance<string>();
            var decl = this.StructBody(first.Location, name);

            this.Advance(TokenKind.Semicolon);

            return decl;
        }

        private ISyntax Declaration() {
            if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionDeclaration();
            }
            else if (this.Peek(TokenKind.StructKeyword)) {
                return this.StructDeclaration();
            }

            throw new Exception();
        }

        private ISyntax TopExpression() => this.StoreExpression();

        private ISyntax StoreExpression() {
            var target = this.OrExpression();

            while (true) {
                if (this.TryAdvance(TokenKind.LeftArrow)) {
                    var value = this.TopExpression();

                    target = new VariableStoreSyntax() {
                        Target = target,
                        Value = value
                    };
                }
                else {
                    return target;
                }
            }
        }

        private ISyntax OrExpression() {
            var first = this.XorExpression();

            while (this.Peek(TokenKind.OrKeyword)) {
                var second = this.XorExpression();

                first = new BinarySyntax() {
                    Left = first,
                    Right = second,
                    Operation = BinarySyntaxOperation.Or
                };
            }

            return first;
        }

        private ISyntax XorExpression() {
            var first = this.ComparisonExpression();

            while (this.Peek(TokenKind.XorKeyword)) {
                var second = this.ComparisonExpression();

                first = new BinarySyntax() {
                    Left = first,
                    Right = second,
                    Operation = BinarySyntaxOperation.Or
                };
            }

            return first;
        }

        private ISyntax ComparisonExpression() {
            var first = this.AndExpression();
            var comparators = new Dictionary<TokenKind, BinarySyntaxOperation>() {
                { TokenKind.EqualSign, BinarySyntaxOperation.EqualTo }, { TokenKind.NotEqualSign, BinarySyntaxOperation.NotEqualTo },
                { TokenKind.LessThanSign, BinarySyntaxOperation.LessThan }, { TokenKind.GreaterThanSign, BinarySyntaxOperation.GreaterThan },
                { TokenKind.LessThanOrEqualToSign, BinarySyntaxOperation.LessThanOrEqualTo },
                { TokenKind.GreaterThanOrEqualToSign, BinarySyntaxOperation.GreaterThanOrEqualTo }
            };

            while (true) {
                bool worked = false;

                foreach (var (tok, _) in comparators) {
                    worked |= this.Peek(tok);
                }

                if (!worked) {
                    break;
                }

                var op = this.Advance();
                var second = this.AndExpression();

                first = new BinarySyntax() {
                    Left = first,
                    Right = second,
                    Operation = comparators[op.Kind]
                };
            }

            return first;
        }

        private ISyntax AndExpression() {
            var first = this.AddExpression();

            while (this.Peek(TokenKind.AndKeyword)) {
                var second = this.AddExpression();

                first = new BinarySyntax() {
                    Left = first,
                    Right = second,
                    Operation = BinarySyntaxOperation.And
                };
            }

            return first;
        }

        private ISyntax AddExpression() {
            var first = this.MultiplyExpression();

            while (true) {
                if (!this.Peek(TokenKind.AddSign) && !this.Peek(TokenKind.SubtractSign)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.AddSign ? BinarySyntaxOperation.Add : BinarySyntaxOperation.Subtract;
                var second = this.MultiplyExpression();

                first = new BinarySyntax() {
                    Left = first,
                    Right = second,
                    Operation = op
                };
            }

            return first;
        }

        private ISyntax MultiplyExpression() {
            var first = this.MemberUsageSyntax();

            while (true) {
                if (!this.Peek(TokenKind.MultiplySign)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.MultiplySign ? BinarySyntaxOperation.Multiply : BinarySyntaxOperation.Multiply;
                var second = this.MemberUsageSyntax();

                first = new BinarySyntax() {
                    Left = first,
                    Right = second,
                    Operation = op
                };
            }

            return first;
        }        

        private ISyntax MemberUsageSyntax() {
            var first = this.InvokeExpression();

            if (this.Peek(TokenKind.Dot)) {
                var segs = new List<IMemberUsageSegment>();

                while (true) {
                    if (this.TryAdvance(TokenKind.Dot)) {
                        var name = this.Advance<string>();

                        if (this.TryAdvance(TokenKind.OpenParenthesis)) {
                            var args = new List<ISyntax>();

                            while (!this.TryAdvance(TokenKind.CloseParenthesis)) {
                                args.Add(this.TopExpression());

                                if (!this.Peek(TokenKind.CloseParenthesis)) {
                                    this.Advance(TokenKind.Comma);
                                }
                            }

                            segs.Add(new MemberInvokeSegment(name, args.ToArray()));
                        }
                        else {
                            segs.Add(new MemberAccessSegment(name));
                        }
                    }
                    else {
                        break;
                    }
                }

                return new MemberUsageSyntax() {
                    Target = first,
                    Segments = segs.ToArray()
                };
            }
            else {
                return first;
            }
        }

        private ISyntax InvokeExpression() {
            var first = this.Atom();

            while (true) {
                if (this.TryAdvance(TokenKind.OpenBracket)) {
                    var inner = this.TopExpression();
                    this.Advance(TokenKind.CloseBracket);

                    first = new IndexAccess() {
                        Target = first,
                        Index = inner
                    };
                }
                else if (this.TryAdvance(TokenKind.OpenParenthesis)) {
                    var args = new List<ISyntax>();

                    while (!this.Peek(TokenKind.CloseParenthesis)) {
                        args.Add(this.TopExpression());

                        if (!this.Peek(TokenKind.CloseParenthesis)) {
                            this.Advance(TokenKind.Comma);
                        }
                    }

                    this.Advance(TokenKind.CloseParenthesis);

                    first = new FunctionInvoke() {
                        Target = first,
                        Arguments = args.ToArray()
                    };
                }
                else {
                    return first;
                }
            }
        }

        private ISyntax VariableDeclarationStatement() {
            var tok = this.Advance(TokenKind.VarKeyword);
            var name = this.Advance<string>();
            var op = this.Advance(TokenKind.LeftArrow);

            //if (op.Kind == TokenKind.LeftArrow) {
            //    kind = VariableInitKind.Store;
            //}
            //else if (op.Kind == TokenKind.AssignmentSign) {
               // kind = VariableInitKind.Equate;
            //}
            //else {
           //     throw ParsingErrors.UnexpectedToken(op);
           // }

            var value = this.TopExpression();

            return new VariableInitSyntax() {
                VariableName = name,
                Value = value
            };
        }

        private ISyntax WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.DoKeyword);
            this.Advance(TokenKind.Colon);

            var body = this.TopExpression();

            return new WhileSyntax() {
                Condition = cond,
                Body = body
            };
        }

        private ISyntax Statement() {
            ISyntax result;

            if (this.Peek(TokenKind.VarKeyword)) {
                result = this.VariableDeclarationStatement();
            }
            else if (this.Peek(TokenKind.WhileKeyword)) {
                result = this.WhileStatement();
            }
            else {
                result = this.TopExpression();
            }

            this.Advance(TokenKind.Semicolon);

            return result;
        }

        private ISyntax IntLiteral() {
            var tok = (Token<long>)this.Advance(TokenKind.IntLiteral);

            return new IntLiteral() {
                Value = tok.Value
            };
        }

        private ISyntax BlockExpression() {
            this.Advance(TokenKind.OpenBrace);
            var list = new List<ISyntax>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                list.Add(this.Statement());
            }

            this.Advance(TokenKind.CloseBrace);

            return new BlockSyntax() {
                Statements = list.ToArray()
            };
        }

        private ISyntax VariableAccess() {
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);

            return new VariableAccessSyntax() {
                VariableName = tok.Value
            };
        }

        private ISyntax VariableLiteralAccess() {
            this.Advance(TokenKind.LiteralSign);
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);

            return new VariableLiteralAccess() {
                VariableName = tok.Value
            };
        }

        private ISyntax VoidLiteral() {
            this.Advance(TokenKind.VoidKeyword);

            return new VoidLiteral();
        }

        private ISyntax ParenGroup() {
            this.Advance(TokenKind.OpenParenthesis);

            var result = this.TopExpression();

            this.Advance(TokenKind.CloseParenthesis);

            return result;
        }

        private ISyntax BoolLiteral() {
            var value = this.Advance<bool>();

            return new BoolLiteral() {
                Value = value
            };
        }

        private ISyntax ArrayLiteral() {
            this.Advance(TokenKind.OpenBracket);
            var elems = new List<ISyntax>();

            while (!this.Peek(TokenKind.CloseBracket)) {
                elems.Add(this.TopExpression());

                if (!this.Peek(TokenKind.CloseBracket)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            this.Advance(TokenKind.CloseBracket);

            return new ArrayLiteralSyntax() {
                Values = elems.ToArray()
            };
        }

        private ISyntax MoveExpression() {
            this.Advance(TokenKind.MoveKeyword);

            var name = this.Advance<string>();

            return new MoveSyntax() {
                VariableName = name
            };
        }

        private ISyntax IfExpression() {
            this.Advance(TokenKind.IfKeyword);

            var cond = this.TopExpression();

            this.Advance(TokenKind.ThenKeyword);
            this.Advance(TokenKind.Colon);

            var affirm = this.TopExpression();

            this.Advance(TokenKind.ElseKeyword);
            this.Advance(TokenKind.Colon);

            var neg = this.TopExpression();

            return new IfExpression() {
                Condition = cond,
                Affirmative = affirm,
                Negative = neg
            };
        }

        private NamedArgument MemberInstantiation() {
            var start = (Token<string>)this.Advance(TokenKind.Identifier);

            this.Advance(TokenKind.AssignmentSign);
            var value = this.TopExpression();

            return new NamedArgument() {
                Name = start.Value,
                Value = value
            };
        }

        private ISyntax NewExpression() {
            var start = this.Advance(TokenKind.NewKeyword);
            var type = this.TypeExpression();

            if (this.TryAdvance(TokenKind.OpenBrace)) {
                // Parse new struct
                var args = new List<NamedArgument>();

                while (!this.Peek(TokenKind.CloseBrace)) {
                    args.Add(this.MemberInstantiation());

                    if (!this.Peek(TokenKind.CloseBrace)) {
                        this.Advance(TokenKind.Comma);
                    }
                }

                this.Advance(TokenKind.CloseBrace);

                return new StructLiteral() {
                    TargetType = type,
                    Arguments = args.ToArray()
                };
            }
            else {
                throw new Exception();
            }
        }


        private ISyntax Atom() {
            if (this.Peek(TokenKind.IntLiteral)) {
                return this.IntLiteral();
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.BlockExpression();
            }
            else if (this.Peek(TokenKind.Identifier)) {
                return this.VariableAccess();
            }
            else if (this.Peek(TokenKind.VoidKeyword)) {
                return this.VoidLiteral();
            }
            else if (this.Peek(TokenKind.OpenParenthesis)) {
                return this.ParenGroup();
            }
            else if (this.Peek(TokenKind.BoolLiteral)) {
                return this.BoolLiteral();
            }
            else if (this.Peek(TokenKind.OpenBracket)) {
                return this.ArrayLiteral();
            }
            else if (this.Peek(TokenKind.MoveKeyword)) {
                return this.MoveExpression();
            }
            else if (this.Peek(TokenKind.LiteralSign)) {
                return this.VariableLiteralAccess();
            }
            else if (this.Peek(TokenKind.IfKeyword)) {
                return this.IfExpression();
            }
            else if (this.Peek(TokenKind.NewKeyword)) {
                return this.NewExpression();
            }
            else {
                var next = this.Advance();

                //throw ParsingErrors.UnexpectedToken(next);
                throw new Exception();
            }
        }
    }
}