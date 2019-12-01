using Attempt2.Lexing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt2.Parsing {
    public class Parser {
        private Lexer lexer;
        private int pos = 0;
        private IReadOnlyList<Token> tokens;

        public Parser(Lexer lexer) {
            this.lexer = lexer;
        }

        public IAST Parse() {
            List<Token> tokens = new List<Token>();

            while (true) {
                var next = this.lexer.NextToken();

                if (next == Token.EOF) {
                    break;
                }

                tokens.Add(next);                
            }

            this.tokens = tokens;
            return this.AddExpr();
        }

        private Token Peek() {
            if (this.pos >= this.tokens.Count) {
                return Token.EOF;
            }

            return this.tokens[this.pos];
        }

        private Token Advance() {
            if (this.pos >= this.tokens.Count) {
                return Token.EOF;
            }

            return this.tokens[this.pos++];
        }

        private Token Advance(Token token) {
            if (this.pos >= this.tokens.Count) {
                return Token.EOF;
            }

            if (this.tokens[this.pos] != token) {
                CompilerErrors.UnexpectedToken(this.tokens[this.pos]);
                return null;
            }

            return this.tokens[this.pos++];
        }

        private T Advance<T>() where T : Token {
            if (this.pos >= this.tokens.Count) {
                CompilerErrors.UnexpectedToken(Token.EOF);
                return default;
            }

            var tok = this.tokens[this.pos++];
            if (tok is T t) {
                return t;
            }
            else {
                CompilerErrors.UnexpectedToken<T>(tok);
                return default;
            }
        }

        private IAST Expression() {
            if (this.Peek() == Token.LetKeyword) {
                return this.VariableDeclarationExpression();
            }
            else if (this.Peek() == Token.IfKeyword) {
                return this.IfExpression();
            }

            return this.AddExpr();
        }

        private IAST VariableDeclarationExpression() {
            var letResult = this.Advance(Token.LetKeyword);
            var nameResult = this.Advance<IdentifierToken>();
            var equalsResult = this.Advance(Token.EqualSign);
            var assignResult = this.Expression();
            var appendixResult = this.Expression();

            return new VariableDeclaration(nameResult.Value, assignResult, appendixResult);
        }

        private IAST IfExpression() {
            var ifResult = this.Advance(Token.IfKeyword);
            var condResult = this.Expression();
            var thenResult = this.Advance(Token.ThenKeyword);
            var ifTrueResult = this.Expression();
            var elseResult = this.Advance(Token.ElseKeyword);
            var ifFalseResult = this.Expression();

            return new IfExpression(condResult, ifTrueResult, ifFalseResult);
        }

        private IAST AddExpr() {
            var first = this.MultExpr();

            while (this.Peek() == Token.AdditionSign || this.Peek() == Token.SubtractionSign) {
                var opToken = this.Advance();
                BinaryOperator op;

                if (opToken == Token.AdditionSign) {
                    op = BinaryOperator.Add;
                }
                else if (opToken == Token.SubtractionSign) {
                    op = BinaryOperator.Subtract;
                }
                else {
                    CompilerErrors.UnexpectedToken(opToken);
                    return null;
                }

                first = new BinaryExpression(op, first, this.MultExpr());
            }

            return first;
        }

        private IAST MultExpr() {
            var first = this.Atom();

            while (this.Peek() == Token.MultiplicationSign || this.Peek() == Token.DivisionSign) {
                var opToken = this.Advance();
                BinaryOperator op;

                if (opToken == Token.MultiplicationSign) {
                    op = BinaryOperator.Multiply;
                }
                else if (opToken == Token.DivisionSign) {
                    op = BinaryOperator.Divide;
                }
                else {
                    CompilerErrors.UnexpectedToken(opToken);
                    return null;
                }

                first = new BinaryExpression(op, first, this.Atom());
            }

            return first;
        }

        private IAST Atom() {            
            if (this.Peek() is IntToken intTok) {
                this.Advance();
                return new Int32Literal(intTok.Value);
            }
            else if (this.Peek() is BoolToken boolTok) {
                this.Advance();
                return new BoolLiteral(boolTok.Value);
            }
            else if (this.Peek() == Token.OpenParenthesis || this.Peek() == Token.OpenBrace) {
                var close = this.Advance() == Token.OpenParenthesis ? Token.CloseParenthesis : Token.CloseBrace;
                var result = this.AddExpr();
                this.Advance(close);

                return result;
            }
            else if (this.Peek() == Token.AdditionSign || this.Peek() == Token.SubtractionSign) {
                var opTok = this.Advance();
                UnaryOperator op;

                if (opTok == Token.AdditionSign) {
                    op = UnaryOperator.Posate;
                }
                else if (opTok == Token.SubtractionSign) {
                    op = UnaryOperator.Negate;
                }
                else {
                    CompilerErrors.UnexpectedToken(opTok);
                    return null;
                }

                return new UnaryExpression(op, this.Atom());
            }
            else if (this.Peek() is IdentifierToken identify) {
                this.Advance();
                return new VariableUsage(identify.Value);
            }
            else {
                return this.Expression();
            }
        }
    }
}