using Attempt7.Lexing;
using Attempt7.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt7.Parsing {
    public class Parser {
        private Lexer lexer;
        private int pos = 0;
        private IReadOnlyList<Token> tokens;

        public Parser(Lexer lexer) {
            this.lexer = lexer;
        }

        public ISymbol Parse() {
            List<Token> tokens = new List<Token>();

            while (true) {
                var next = this.lexer.NextToken();

                if (next.Kind == TokenKind.EOF) {
                    break;
                }

                tokens.Add(next);
            }

            this.tokens = tokens;
            return this.AddExpr();
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

        private ISymbol Expression() {
            if (this.Peek(TokenKind.LetKeyword)) {
                return this.LetExpression();
            }

            return this.AddExpr();
        }

        private ISymbol LetExpression() {
            this.Advance(TokenKind.LetKeyword);

            string name = this.Advance<string>();
            this.Advance(TokenKind.EqualsSign);

            var assign = this.Expression();

            return new LetExpression(name, assign);
        }

        private ISymbol FunctionExpression() {
            this.Advance(TokenKind.FunctionKeyword);
            this.Advance(TokenKind.OpenParenthesis);

            List<FunctionParameterDefinition> pars = new List<FunctionParameterDefinition>();
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                string type = this.Advance<string>();
                string name = this.Advance<string>();

                pars.Add(new FunctionParameterDefinition(type, name));
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.Arrow);

            return new FunctionDefinition(this.Expression(), pars);
        }

        private ISymbol Block() {
            this.Advance(TokenKind.OpenBrace);

            ISymbol BlockExpression() {
                var first = this.Expression();
                this.Advance(TokenKind.Semicolon);

                if (this.Peek(TokenKind.CloseBrace)) {
                    this.Advance(TokenKind.CloseBrace);
                    return first;
                }
                else {
                    return new Statement(first, BlockExpression());
                }
            }

            return BlockExpression();
        }

        private ISymbol AddExpr() {
            var first = this.MultExpr();

            while (this.Peek().Kind == TokenKind.AdditionSign || this.Peek().Kind == TokenKind.SubtractionSign) {
                var opToken = this.Advance();
                BinaryOperator op;

                if (opToken.Kind == TokenKind.AdditionSign) {
                    op = BinaryOperator.Addition;
                }
                else if (opToken.Kind == TokenKind.SubtractionSign) {
                    op = BinaryOperator.Subtraction;
                }
                else {
                    throw new Exception($"Unexpected addition operator '{opToken}'");
                }

                first = new BinaryOperation(op, first, this.MultExpr());
            }

            return first;
        }

        private ISymbol MultExpr() {
            var first = this.Atom();

            while (this.Peek().Kind == TokenKind.MultiplicationSign || this.Peek().Kind == TokenKind.DivisionSign) {
                var opToken = this.Advance();
                BinaryOperator op;

                if (opToken.Kind == TokenKind.MultiplicationSign) {
                    op = BinaryOperator.Multiplication;
                }
                else if (opToken.Kind == TokenKind.DivisionSign) {
                    op = BinaryOperator.Division;
                }
                else {
                    throw new Exception($"Unexpected multiplication operator '{opToken}'");
                }

                first = new BinaryOperation(op, first, this.Atom());
            }

            return first;
        }

        private ISymbol Atom() {
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
            else if (this.Peek(TokenKind.SubtractionSign)) {
                this.Advance(TokenKind.SubtractionSign);
                return new BinaryOperation(
                    BinaryOperator.Subtraction,
                    new Int32Literal(0),
                    this.Atom()
                );
            }
            else if (this.Peek(TokenKind.AdditionSign)) {
                this.Advance(TokenKind.AdditionSign);
                return this.Atom();
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.Block();
            }
            else if (this.Peek(TokenKind.Identifier)) {
                string name = this.Advance<string>();
                return new IdentifierLiteral(name);
            }
            else {
                throw new Exception();
            }
        }
    }
}