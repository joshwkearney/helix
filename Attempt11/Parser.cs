using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt10 {
    public class Parser {
        private Lexer lexer;
        private Analyzer ana = new Analyzer();

        private int pos = 0;
        private IReadOnlyList<Token> tokens;

        public Parser(Lexer lexer) {
            this.lexer = lexer;
        }

        public ISyntaxTree Parse() {
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

        private ITrophyType TypeExpression() {
            return this.TypeFunctionExpression();
        }

        private ITrophyType TypeFunctionExpression() {
            var args = new List<ITrophyType> { this.TypeAtom() };

            while (this.Peek(TokenKind.MultiplicationSign)) {
                this.Advance(TokenKind.MultiplicationSign);
                args.Add(this.TypeAtom());
            }

            if (!this.Peek(TokenKind.Arrow)) {
                return args[0];
            }

            this.Advance(TokenKind.Arrow);
            var ret = this.TypeAtom();

            return new ClosureTrophyType(new FunctionTrophyType(ret, args));
        }

        private ITrophyType TypeAtom() {
            string type = this.Advance<string>();

            if (type == "int64") {
                return PrimitiveTrophyType.Int64Type;
            }
            else if (type == "bool") {
                return PrimitiveTrophyType.Boolean;
            }
            else if (type == "real64") {
                return PrimitiveTrophyType.Real64Type;
            }

            throw new Exception();
        }
        
        private ISyntaxTree Expression() {
            if (this.Peek(TokenKind.IfKeyword)) {
                return this.IfExpression();
            }
            else if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionExpression();
            }

            return this.OrExpression();
        }

        private ISyntaxTree IfExpression() {
            this.Advance(TokenKind.IfKeyword);
            var condition = this.Expression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.Expression();

            this.Advance(TokenKind.ElseKeyword);
            var neg = this.Expression();

            return this.ana.AnalyzeIfExpression(condition, affirm, neg);
        }

        private ISyntaxTree FunctionExpression() {
            this.Advance(TokenKind.FunctionKeyword);

            var returnType = this.TypeExpression();

            this.Advance(TokenKind.OpenParenthesis);

            var argNames = new List<string>();
            var argTypes = new List<ITrophyType>();

            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var type = this.TypeExpression();
                string name = this.Advance<string>();

                argNames.Add(name);
                argTypes.Add(type);

                if (this.Peek(TokenKind.Comma)) {
                    this.Advance(TokenKind.Comma);
                }
                else {
                    break;
                }
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.Arrow);

            return this.ana.AnalyzeFunctionLiteral(argNames, argTypes, returnType, this.Expression());
        }

        private ISyntaxTree Block() {
            ISyntaxTree BlockExpression() {
                if (this.Peek(TokenKind.LetKeyword)) {
                    return LetExpression();
                }
                else {
                    var result = this.Expression();

                    if (this.Peek(TokenKind.Semicolon)) {
                        this.Advance(TokenKind.Semicolon);
                    }

                    return result;
                }
            }

            ISyntaxTree LetExpression() {
                this.Advance(TokenKind.LetKeyword);
                string name = this.Advance<string>();

                this.Advance(TokenKind.EqualsSign);
                var assign = this.Expression();
                this.Advance(TokenKind.Semicolon);

                return this.ana.AnalyzeConstantDefinition(name, assign, () => BlockExpression());
            }

            this.Advance(TokenKind.OpenBrace);
            var ret = BlockExpression();
            this.Advance(TokenKind.CloseBrace);

            return ret;
        }

        private ISyntaxTree OrExpression() {
            var mult = this.XorExpression();

            while (this.Peek(TokenKind.OrKeyword)) {
                this.Advance(TokenKind.OrKeyword);
                mult = this.ana.AnalyzeBinaryOr(mult, this.XorExpression());
            }

            return mult;
        }

        private ISyntaxTree XorExpression() {
            var mult = this.AndExpression();

            while (this.Peek(TokenKind.XorKeyword)) {
                this.Advance(TokenKind.XorKeyword);
                mult = this.ana.AnalyzeBinaryXor(mult, this.AndExpression());
            }

            return mult;
        }

        private ISyntaxTree AndExpression() {
            var mult = this.AddExpression();

            while (this.Peek(TokenKind.AndKeyword)) {
                this.Advance(TokenKind.AndKeyword);
                mult = this.ana.AnalyzeBinaryAnd(mult, this.AddExpression());
            }

            return mult;
        }

        private ISyntaxTree AddExpression() {
            var mult = this.MultiplyExpression();

            while (this.Peek(TokenKind.AdditionSign) || this.Peek(TokenKind.SubtractionSign)) {
                if (this.Advance().Kind == TokenKind.AdditionSign) {
                    mult = this.ana.AnalyzeBinaryAddition(mult, this.MultiplyExpression());
                }
                else {
                    mult = this.ana.AnalyzeBinarySubtraction(mult, this.MultiplyExpression());
                }
            }

            return mult;
        }

        private ISyntaxTree MultiplyExpression() {
            var invoke = this.NotExpression();

            while (this.Peek(TokenKind.MultiplicationSign) || this.Peek(TokenKind.RealDivisionSign) || this.Peek(TokenKind.StrictDivisionSign)) {
                var advance = this.Advance().Kind;

                if (advance == TokenKind.MultiplicationSign) {
                    invoke = this.ana.AnalyzeBinaryMultiplication(invoke, this.NotExpression());
                }
                else if (advance == TokenKind.RealDivisionSign) {
                    invoke = this.ana.AnalyzeBinaryRealDivision(invoke, this.NotExpression());
                }
                else {
                    invoke = this.ana.AnalyzeBinaryStrictDivision(invoke, this.NotExpression());
                }
            }

            return invoke;
        }

        private ISyntaxTree NotExpression() {
            if (this.Peek(TokenKind.NotSign)) {
                this.Advance(TokenKind.NotSign);
                return this.ana.AnalyzeUnaryNot(this.InvokeExpression());
            }
            else if (this.Peek(TokenKind.SubtractionSign)) {
                this.Advance(TokenKind.SubtractionSign);
                return this.ana.AnalyzeUnaryNegation(this.InvokeExpression());
            }

            return this.InvokeExpression();
        }        

        private ISyntaxTree InvokeExpression() {
            var atom = this.Atom();

            while (this.Peek(TokenKind.OpenParenthesis)) {
                var args = new List<ISyntaxTree>();
                this.Advance(TokenKind.OpenParenthesis);

                while (!this.Peek(TokenKind.CloseParenthesis)) {
                    args.Add(this.Expression());

                    if (this.Peek(TokenKind.Comma)) {
                        this.Advance(TokenKind.Comma);
                    }
                }

                this.Advance(TokenKind.CloseParenthesis);

                atom = this.ana.AnalyzeInvoke(atom, args);
            }

            return atom;
        }

        private ISyntaxTree Atom() {
            if (this.Peek() is Token<long> intTok) {
                this.Advance();
                return this.ana.AnalyzeInt64Literal(intTok.Value);
            }
            else if (this.Peek() is Token<bool> boolTok) {
                this.Advance();
                return this.ana.AnalyzeBoolLiteral(boolTok.Value);
            }
            else if (this.Peek() is Token<double> realTok) {
                this.Advance();
                return this.ana.AnalyzeReal64Literal(realTok.Value);
            }
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
                return this.ana.AnalyzeVariableLiteral(name);
            }
            else {
                throw new Exception();
            }
        }
    }
}