using Attempt16.Analysis;
using Attempt16.Syntax;
using Attempt16.Types;
using System;
using System.Collections.Generic;

namespace Attempt16.Parsing {
    public class Parser {
        private int pos = 0;
        private readonly IReadOnlyList<IToken> tokens;

        public Parser(IReadOnlyList<IToken> tokens) {
            this.tokens = tokens;
        }

        public IReadOnlyList<IDeclaration> Parse() {
            var list = new List<IDeclaration>();

            while (this.pos < this.tokens.Count) {
                list.Add(this.Declaration());
            }

            return list;
        }

        private FunctionDeclaration FunctionDeclaration() {
            this.Advance(TokenKind.FunctionKeyword);

            string funcName = this.Advance<string>();
            this.Advance(TokenKind.OpenParenthesis);

            var pars = new List<FunctionParameter>();
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var parType = this.TypeExpression();
                var parName = this.Advance<string>();

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars.Add(new FunctionParameter() {
                    Name = parName,
                    TypePath = parType
                });
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.YieldSign);

            var returnType = this.TypeExpression();
            this.Advance(TokenKind.Colon);

            var body = this.TopExpression();

            return new FunctionDeclaration() {
                Name = funcName,
                Parameters = pars,
                ReturnType = returnType,
                Body = body
            };
        }

        private StructDeclaration StructDeclaration() {
            this.Advance(TokenKind.StructKeyword);

            string name = this.Advance<string>();
            this.Advance(TokenKind.OpenBrace);

            var mems = new List<StructMember>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                var type = this.TypeExpression();
                var memName = this.Advance<string>();

                mems.Add(new StructMember() {
                    Name = memName,
                    TypePath = type
                });
            }

            this.Advance(TokenKind.CloseBrace);

            return new StructDeclaration() {
                Name = name,
                Members = mems
            };
        }

        private IDeclaration Declaration() {
            if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionDeclaration();
            }
            else if (this.Peek(TokenKind.StructKeyword)) {
                return this.StructDeclaration();
            }

            throw new Exception();
        }

        private ISyntax TopExpression() => this.AdditionExpression();

        private bool Peek(TokenKind kind) {
            if (this.pos >= this.tokens.Count) {
                return false;
            }

            return this.tokens[this.pos].Kind == kind;
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
                throw new Exception();
            }

            return this.tokens[this.pos++];
        }

        private IToken Advance(TokenKind kind) {
            if (this.pos >= this.tokens.Count) {
                throw new Exception();
            }

            if (this.tokens[this.pos].Kind != kind) {
                CompilerErrors.UnexpectedToken(this.tokens[this.pos]);
                return null;
            }

            return this.tokens[this.pos++];
        }

        private T Advance<T>() {
            if (this.pos >= this.tokens.Count) {
                throw new Exception();
            }

            var tok = this.tokens[this.pos++];
            if (tok is Token<T> t) {
                return t.Value;
            }
            else {
                CompilerErrors.UnexpectedToken<T>(tok);
                return default;
            }
        }

        private IdentifierPath TypeExpression() {
            if (this.Peek(TokenKind.VarKeyword)) {
                return this.VarTypeExpression();
            }

            return this.TypeAtom();
        }

        private IdentifierPath VarTypeExpression() {
            this.Advance(TokenKind.VarKeyword);
            var path = this.TypeExpression();

            return new IdentifierPath("%var").Append(path);
        }

        private IdentifierPath TypeAtom() {
            if (this.Peek(TokenKind.IntKeyword)) {
                this.Advance();
                return new IdentifierPath("int");
            }
            else if (this.Peek(TokenKind.VoidKeyword)) {
                this.Advance();
                return new IdentifierPath("void");
            }
            else {
                return new IdentifierPath(this.Advance<string>());
            }
        }

        private ISyntax Statement() {
            if (this.Peek(TokenKind.VarKeyword)) {
                return this.VariableDeclarationStatement();
            }
            else if (this.Peek(TokenKind.IfKeyword)) {
                return this.IfStatement();
            }
            else if (this.Peek(TokenKind.WhileKeyword)) {
                return this.WhileStatement();
            }

            return this.TopExpression();
        }

        public ISyntax IfStatement() {
            this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.TopExpression();

            ISyntax neg = null;
            if (this.Peek(TokenKind.ElseKeyword)) {
                this.Advance(TokenKind.ElseKeyword);
                neg = this.TopExpression();
            }

            return new IfSyntax() {
                Condition = cond,
                Affirmative = affirm,
                Negative = neg
            };
        }

        public ISyntax WhileStatement() {
            this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.DoKeyword);
            var body = this.TopExpression();            

            return new WhileStatement() {
                Condition = cond,
                Body = body
            };
        }

        private ISyntax VariableDeclarationStatement() {
            int varCount = 0;

            while (this.Peek(TokenKind.VarKeyword)) {
                varCount++;
                this.Advance(TokenKind.VarKeyword);
            }

            string name = this.Advance<string>();

            DeclarationOperation op;
            if (this.Peek(TokenKind.LeftArrow)) {
                op = DeclarationOperation.Store;
            }
            else if (this.Peek(TokenKind.EqualSign)) {
                op = DeclarationOperation.Equate;
            }
            else {
                throw new Exception();
            }

            this.Advance();
            var value = this.TopExpression();

            return new VariableStatement() {
                VariableName = name,
                Value = value,
                Operation = op,
                VarCount = varCount
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
                Statements = list
            };
        }

        private ISyntax AdditionExpression() {
            var first = this.MultiplicationExpression();

            while (this.Peek(TokenKind.AddSign) || this.Peek(TokenKind.SubtractSign)) {
                bool add = this.Advance().Kind == TokenKind.AddSign;

                var second = this.MultiplicationExpression();

                first = new BinaryExpression() {
                    Left = first,
                    Right = second,
                    Operation = add ? BinaryOperator.Add : BinaryOperator.Subtract
                };
            }

            return first;
        }

        private ISyntax MultiplicationExpression() {
            var first = this.CopyExpression();

            while (this.Peek(TokenKind.MultiplySign)) {
                this.Advance(TokenKind.MultiplySign);

                var second = this.CopyExpression();

                first = new BinaryExpression() {
                    Left = first,
                    Right = second,
                    Operation = BinaryOperator.Multiply
                };
            }

            return first;
        }

        private ISyntax CopyExpression() {
            if (this.Peek(TokenKind.CopyKeyword)) {
                this.Advance();

                var first = this.CopyExpression();

                return new ValueofSyntax() {
                    Value = first
                };
            }

            return this.StoreExpression();
        }

        private ISyntax StoreExpression() {
            var first = this.InvokeExpression();

            if (this.Peek(TokenKind.LeftArrow)) {
                this.Advance(TokenKind.LeftArrow);

                first = new StoreSyntax() {
                    Target = first,
                    Value = this.TopExpression()
                };
            }

            return first;
        }

        private ISyntax InvokeExpression() {
            var first = this.MemberAccessExpression();

            while (this.Peek(TokenKind.OpenParenthesis)) {
                this.Advance(TokenKind.OpenParenthesis);

                var args = new List<ISyntax>();
                while (!this.Peek(TokenKind.CloseParenthesis)) {
                    args.Add(this.TopExpression());
                    
                    if (!this.Peek(TokenKind.CloseParenthesis)) {
                        this.Advance(TokenKind.Comma);
                    }
                }

                this.Advance(TokenKind.CloseParenthesis);

                first = new FunctionCallSyntax() {
                    Arguments = args,
                    Target = first
                };
            }

            return first;
        }

        private ISyntax MemberAccessExpression() {
            var first = this.Atom();

            while (this.TryAdvance(TokenKind.Dot)) {
                bool literal = this.TryAdvance(TokenKind.LiteralSign);
                string name = this.Advance<string>();

                first = new MemberAccessSyntax() {
                    IsLiteralAccess = literal,
                    MemberName = name,
                    Target = first
                };
            }

            return first;
        }

        private ISyntax Atom() {
            if (this.Peek(TokenKind.IntLiteral)) {
                long value = this.Advance<long>();

                return new IntLiteral() {
                    Value = value
                };
            }           
            else if (this.Peek(TokenKind.OpenParenthesis)) {
                this.Advance(TokenKind.OpenParenthesis);
                var result = this.Statement();
                this.Advance(TokenKind.CloseParenthesis);

                return result;
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.BlockExpression();
            }
            else if (this.Peek(TokenKind.LiteralSign)) {
                this.Advance();
                string name = this.Advance<string>();

                return new VariableLocationLiteral() {
                    VariableName = name
                };
            }
            else if (this.Peek(TokenKind.Identifier)) {
                string id = this.Advance<string>();

                return new VariableLiteral() {
                    VariableName = id
                };
            }
            else if (this.Peek(TokenKind.NewKeyword)) {
                this.Advance(TokenKind.NewKeyword);

                string name = this.Advance<string>();
                this.Advance(TokenKind.OpenBrace);

                var mems = new List<StructMemberInitialization>();
                while (!this.Peek(TokenKind.CloseBrace)) {
                    var memName = this.Advance<string>();

                    DeclarationOperation op;
                    if (this.TryAdvance(TokenKind.EqualSign)) {
                        op = DeclarationOperation.Equate;
                    }
                    else {
                        this.Advance(TokenKind.LeftArrow);
                        op = DeclarationOperation.Store;
                    }

                    var memValue = this.TopExpression();

                    mems.Add(new StructMemberInitialization() {
                        MemberName = memName,
                        Value = memValue,
                        Operation = op
                    });
                }

                this.Advance(TokenKind.CloseBrace);

                return new StructInitializationSyntax() {
                    Members = mems,
                    StructName = name
                };
            }
            else {
                throw new Exception();
            }
        }
    }
}