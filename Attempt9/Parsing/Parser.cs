using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt9 {
    public class Parser {
        private Lexer lexer;
        private int pos = 0;
        private IReadOnlyList<Token> tokens;

        public Parser(Lexer lexer) {
            this.lexer = lexer;
        }

        public IParseTree Parse() {
            List<Token> tokens = new List<Token>();

            while (true) {
                var next = this.lexer.NextToken();

                if (next.Kind == TokenKind.EOF) {
                    break;
                }

                tokens.Add(next);
            }

            this.tokens = tokens;
            return this.Block();
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
                throw new Exception($"Expected '{typeof(T).Name}', got '{tok}'");
            }
        }

        private ITrophyType TypeExpression() => this.TypeAtom();

        private ITrophyType TypeAtom() {
            if (this.Peek() is Token<string> strTok) {
                if (strTok.Value == "int64") {
                    return PrimitiveTrophyType.Int64Type;
                }
            }

            throw new Exception();
        }
        
        private IParseTree Expression() {
            if (this.Peek(TokenKind.IfKeyword)) {
                return this.IfExpression();
            }

            return this.AddExpression();
        }

        private IParseTree IfExpression() {
            this.Advance(TokenKind.IfKeyword);
            var condition = this.Expression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.Expression();

            this.Advance(TokenKind.ElseKeyword);
            var neg = this.Expression();

            return new IfExpression(condition, affirm, neg);
        }

        private IParseTree Block() {
            IParseTree BlockExpression() {
                if (this.Peek(TokenKind.LetKeyword)) {
                    return LetExpression();
                }
                else {
                    return this.Expression();
                }
            }

            IParseTree LetExpression() {
                this.Advance(TokenKind.LetKeyword);
                string name = this.Advance<string>();

                this.Advance(TokenKind.EqualsSign);
                var assign = this.Expression();
                this.Advance(TokenKind.Semicolon);

                var scope = BlockExpression();

                return new VariableDefinition(name, assign, scope);
            }

            this.Advance(TokenKind.OpenBrace);
            var ret = BlockExpression();
            this.Advance(TokenKind.CloseBrace);

            return ret;
        }

        //private IParseTree OrExpression() {
        //    var mult = this.XorExpression();

        //    while (this.Peek(TokenKind.OrKeyword)) {
        //        this.Advance(TokenKind.OrKeyword);
        //        mult = new BinaryExpression(mult, this.XorExpression(), BinaryOperator.Or);
        //    }

        //    return mult;
        //}

        //private IParseTree XorExpression() {
        //    var mult = this.AndExpression();

        //    while (this.Peek(TokenKind.XorKeyword)) {
        //        this.Advance(TokenKind.XorKeyword);
        //        mult = new BinaryExpression(mult, this.AndExpression(), BinaryOperator.Xor);
        //    }

        //    return mult;
        //}

        //private IParseTree AndExpression() {
        //    var mult = this.AddExpression();

        //    while (this.Peek(TokenKind.AndKeyword)) {
        //        this.Advance(TokenKind.AndKeyword);
        //        mult = new BinaryExpression(mult, this.AddExpression(), BinaryOperator.And);
        //    }

        //    return mult;
        //}

        private IParseTree AddExpression() {
            var mult = this.MultiplyExpression();

            while (this.Peek(TokenKind.AdditionSign) || this.Peek(TokenKind.SubtractionSign)) {
                var op = this.Advance().Kind == TokenKind.AdditionSign
                    ? BinaryOperator.Add
                    : BinaryOperator.Subtract;

                mult = new BinaryExpression(mult, this.MultiplyExpression(), op);
            }

            return mult;
        }

        private IParseTree MultiplyExpression() {
            var invoke = this.BoxExpression();

            while (this.Peek(TokenKind.MultiplicationSign) || this.Peek(TokenKind.DivisionSign)) {
                var op = this.Advance().Kind == TokenKind.MultiplicationSign
                    ? BinaryOperator.Multiply
                    : BinaryOperator.Divide;

                invoke = new BinaryExpression(invoke, this.BoxExpression(), op);
            }

            return invoke;
        }

        private IParseTree BoxExpression() {
            if (this.Peek(TokenKind.BoxKeyword)) {
                this.Advance(TokenKind.BoxKeyword);
                return new UnaryExpression(this.BoxExpression(), UnaryOperator.Box);
            }
            else if (this.Peek(TokenKind.UnboxKeyword)) {
                this.Advance(TokenKind.UnboxKeyword);
                return new UnaryExpression(this.BoxExpression(), UnaryOperator.Unbox);
            }

            return this.NotExpression();
        }

        private IParseTree NotExpression() {
            if (this.Peek(TokenKind.NotSign)) {
                this.Advance(TokenKind.NotSign);
                return new UnaryExpression(this.NotExpression(), UnaryOperator.Not);
            }
            else if (this.Peek(TokenKind.SubtractionSign)) {
                this.Advance(TokenKind.SubtractionSign);
                return new UnaryExpression(this.NotExpression(), UnaryOperator.Negation);
            }

            return this.Atom();
        }        

        private IParseTree Atom() {
            if (this.Peek() is Token<long> intTok) {
                this.Advance();
                return new Int64Literal(intTok.Value);
            }
            //else if (this.Peek() is Token<double> realTok) {
            //    this.Advance();
            //    return new Real64Literal(realTok.Value);
            //}
            //else if (this.Peek() is Token<bool> boolTok) {
            //    this.Advance();
            //    return new BooleanLiteral(boolTok.Value);
            //}
            else if (this.Peek(TokenKind.OpenParenthesis)) {
                this.Advance(TokenKind.OpenParenthesis);
                var result = this.Expression();
                this.Advance(TokenKind.CloseParenthesis);

                return result;
            }
            //else if (this.Peek(TokenKind.SubtractionSign)) {
            //    this.Advance(TokenKind.SubtractionSign);

            //    var next = this.Atom();
            //    return ana => ana.InterpretBinaryExpression(
            //        x => x.InterpretInt32Literal(0),
            //        next,
            //        BinaryOperator.Subtraction
            //    );
            //}
            //else if (this.Peek(TokenKind.AdditionSign)) {
            //    this.Advance(TokenKind.AdditionSign);
            //    return this.Atom();
            //}
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.Block();
            }
            else if (this.Peek(TokenKind.Identifier)) {
                string name = this.Advance<string>();
                return new VariableLiteral(name);
            }
            else {
                throw new Exception();
            }
        }
    }
}