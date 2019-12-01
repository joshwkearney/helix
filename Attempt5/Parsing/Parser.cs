using Attempt6.Lexing;
using Attempt6.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt6.Parsing {
    public class Parser {
        private Lexer lexer;
        private int pos = 0;
        private IReadOnlyList<Token> tokens;

        public Parser(Lexer lexer) {
            this.lexer = lexer;
        }

        public IProtoSyntax Parse() {
            List<Token> tokens = new List<Token>();

            while (true) {
                var next = this.lexer.NextToken();

                if (next.Kind == TokenKind.EOF) {
                    break;
                }

                tokens.Add(next);
            }

            this.tokens = tokens;
            return this.Expression();
        }

        private Token Peek() {
            if (this.pos >= this.tokens.Count) {
                return TokenKind.EOF;
            }

            return this.tokens[this.pos];
        }

        private bool Peek(TokenKind kind) {
            return this.Peek().Kind == kind;
        }

        private Token Advance() {
            if (this.pos >= this.tokens.Count) {
                return TokenKind.EOF;
            }

            return this.tokens[this.pos++];
        }

        private Token Advance(TokenKind token) {
            if (this.pos >= this.tokens.Count) {
                return TokenKind.EOF;
            }

            if (this.tokens[this.pos].Kind != token) {
                throw new Exception($"Expected '{token}', got '{this.tokens[this.pos]}'");
            }

            return this.tokens[this.pos++];
        }

        private T Advance<T>() {
            if (this.pos >= this.tokens.Count) {
                throw new Exception("Unexpected end of file");
            }

            var tok = this.tokens[this.pos++];
            if (tok is Token<T> t) {
                return t.Value;
            }
            else {
                throw new Exception($"Expected '{nameof(T)}', got '{tok}'");
            }
        }

        private IProtoSyntax Expression() {
            return this.AddExpr();
        }

        private IProtoSyntax Block() {
            this.Advance(TokenKind.OpenBrace);

            IProtoSyntax BlockExpression() {
                if (this.Peek(TokenKind.LetKeyword) || this.Peek(TokenKind.VarKeyword)) {
                    return BlockVarAssignment();
                }
                else {
                    var first = this.Expression();
                    this.Advance(TokenKind.Semicolon);

                    if (this.Peek(TokenKind.CloseBrace)) {
                        this.Advance(TokenKind.CloseBrace);
                        return first;
                    }
                    else {
                        return new ProtoStatement(first, BlockExpression());
                    }
                }
            }

            IProtoSyntax BlockVarAssignment() {
                bool isreadonly = this.Peek(TokenKind.LetKeyword);
                this.Advance();

                string name = this.Advance<string>();
                this.Advance(TokenKind.EqualsSign);

                var assign = this.Expression();
                this.Advance(TokenKind.Semicolon);

                IProtoSyntax scope;
                if (this.Peek(TokenKind.CloseBrace)) {
                    scope = new ProtoVariableLiteral(name);
                }
                else {
                    scope = BlockExpression();
                }

                return new ProtoVariableDeclaration(name, isreadonly, assign, scope);
            }

            return BlockExpression();
        }

        private IProtoSyntax AddExpr() {
            var first = this.MultExpr();

            while (this.Peek().Kind == TokenKind.AdditionSign || this.Peek().Kind == TokenKind.SubtractionSign) {
                var opToken = this.Advance();
                BinaryOperator op;

                if (opToken.Kind == TokenKind.AdditionSign) {
                    op = BinaryOperator.Add;
                }
                else if (opToken.Kind == TokenKind.SubtractionSign) {
                    op = BinaryOperator.Subtract;
                }
                else {
                    throw new Exception($"Unexpected addition operator '{opToken}'");
                }

                first = new ProtoBinaryExpression(op, first, this.MultExpr());
            }

            return first;
        }

        private IProtoSyntax MultExpr() {
            var first = this.Atom();

            while (this.Peek().Kind == TokenKind.MultiplicationSign || this.Peek().Kind == TokenKind.DivisionSign) {
                var opToken = this.Advance();
                BinaryOperator op;

                if (opToken.Kind == TokenKind.MultiplicationSign) {
                    op = BinaryOperator.Multiply;
                }
                else if (opToken.Kind == TokenKind.DivisionSign) {
                    op = BinaryOperator.Divide;
                }
                else {
                    throw new Exception($"Unexpected multiplication operator '{opToken}'");
                }

                first = new ProtoBinaryExpression(op, first, this.Atom());
            }

            return first;
        }

        private IProtoSyntax Atom() {
            if (this.Peek() is Token<int> intTok) {
                this.Advance();
                return new Int32Literal(intTok.Value);
            }
            else if (this.Peek(TokenKind.OpenParenthesis)) {
                this.Advance(TokenKind.OpenParenthesis);
                var result = this.Expression();
                this.Advance(TokenKind.CloseParenthesis);

                return result;
            }            
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.Block();
            }
            else if (this.Peek(TokenKind.Identifier)) {
                string name = this.Advance<string>();

                if (this.Peek(TokenKind.EqualsSign)) {
                    this.Advance(TokenKind.EqualsSign);
                    return new ProtoVariableStore(name, this.Expression());
                }
                else {
                    return new ProtoVariableLiteral(name);
                }
            }
            else {
                throw new Exception();
            }
        }
    }
}