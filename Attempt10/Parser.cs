using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt12 {
    public class Parser {
        private Lexer lexer;
        private Analyzer ana = new Analyzer();

        private int pos = 0;
        private IReadOnlyList<Token> tokens;

        public Parser(Lexer lexer) {
            this.lexer = lexer;
        }

        public SyntaxPotential Parse() {
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

            return new TrophyFunctionType(ret, args.ToArray());
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
        
        private SyntaxPotential Expression() {
            if (this.Peek(TokenKind.IfKeyword)) {
                return this.IfExpression();
            }
            else if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionExpression();
            }

            return this.PipeExpression();
        }

        private SyntaxPotential IfExpression() {
            this.Advance(TokenKind.IfKeyword);
            var condition = this.Expression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.Expression();

            this.Advance(TokenKind.ElseKeyword);
            var neg = this.Expression();

            return this.ana.AnalyzeIfExpression(condition, affirm, neg);
        }

        private SyntaxPotential FunctionExpression() {
            this.Advance(TokenKind.FunctionKeyword);

            ITrophyType returnType = null;
            if (!this.Peek(TokenKind.OpenParenthesis)) {
                returnType = this.TypeExpression();
            }

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

            return this.ana.AnalyzeFunctionLiteral(argNames, argTypes, this.Expression(), returnType);
        }

        private SyntaxPotential Block() {
            SyntaxPotential BlockExpression() {
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

            SyntaxPotential LetExpression() {
                this.Advance(TokenKind.LetKeyword);
                string name = this.Advance<string>();

                this.Advance(TokenKind.EqualsSign);
                var assign = this.Expression();
                this.Advance(TokenKind.Semicolon);

                return this.ana.AnalyzeConstantDefinition(name, assign, BlockExpression());
            }

            this.Advance(TokenKind.OpenBrace);
            var ret = BlockExpression();
            this.Advance(TokenKind.CloseBrace);

            return ret;
        }

        private SyntaxPotential PipeExpression() {
            var first = this.OrExpression();

            while (this.Peek(TokenKind.PipeRight)) {
                this.Advance(TokenKind.PipeRight);
                first = this.ana.AnalyzeInvoke(this.OrExpression(), new[] { first });
            }

            return first;
        }

        private SyntaxPotential OrExpression() {
            var mult = this.XorExpression();

            while (this.Peek(TokenKind.OrKeyword)) {
                this.Advance(TokenKind.OrKeyword);
                mult = this.ana.AnalyzeBinaryOperator(mult, this.XorExpression(), "or");
            }

            return mult;
        }

        private SyntaxPotential XorExpression() {
            var mult = this.AndExpression();

            while (this.Peek(TokenKind.XorKeyword)) {
                this.Advance(TokenKind.XorKeyword);
                mult = this.ana.AnalyzeBinaryOperator(mult, this.AndExpression(), "xor");
            }

            return mult;
        }

        private SyntaxPotential AndExpression() {
            var mult = this.ComparisonExpression();

            while (this.Peek(TokenKind.AndKeyword)) {
                this.Advance(TokenKind.AndKeyword);
                mult = this.ana.AnalyzeBinaryOperator(mult, this.ComparisonExpression(), "and");
            }

            return mult;
        }

        private SyntaxPotential ComparisonExpression() {
            var first = this.AddExpression();

            if (this.Peek(TokenKind.GreaterThanSign)) {
                this.Advance(TokenKind.GreaterThanSign);
                first = this.ana.AnalyzeBinaryOperator(first, this.AddExpression(), "greater_than");
            }
            else if (this.Peek(TokenKind.LessThanSign)) {
                this.Advance(TokenKind.LessThanSign);
                first = this.ana.AnalyzeBinaryOperator(first, this.AddExpression(), "less_than");
            }

            return first;
        }

        private SyntaxPotential AddExpression() {
            var mult = this.MultiplyExpression();

            while (this.Peek(TokenKind.AdditionSign) || this.Peek(TokenKind.SubtractionSign)) {
                if (this.Advance().Kind == TokenKind.AdditionSign) {
                    mult = this.ana.AnalyzeBinaryOperator(mult, this.MultiplyExpression(), "add");
                }
                else {
                    mult = this.ana.AnalyzeBinaryOperator(mult, this.MultiplyExpression(), "subtract");
                }
            }

            return mult;
        }

        private SyntaxPotential MultiplyExpression() {
            var invoke = this.NotExpression();

            while (this.Peek(TokenKind.MultiplicationSign) || this.Peek(TokenKind.RealDivisionSign) || this.Peek(TokenKind.StrictDivisionSign)) {
                var advance = this.Advance().Kind;

                if (advance == TokenKind.MultiplicationSign) {
                    invoke = this.ana.AnalyzeBinaryOperator(invoke, this.NotExpression(), "multiply");
                }
                else if (advance == TokenKind.RealDivisionSign) {
                    invoke = this.ana.AnalyzeBinaryOperator(invoke, this.NotExpression(), "divide");
                }
                else {
                    invoke = this.ana.AnalyzeBinaryOperator(invoke, this.NotExpression(), "divide_strict");
                }
            }

            return invoke;
        }

        private SyntaxPotential NotExpression() {
            if (this.Peek(TokenKind.NotSign)) {
                this.Advance(TokenKind.NotSign);
                return this.ana.AnalyzeUnaryOperator(this.InvokeExpression(), "not");
            }
            else if (this.Peek(TokenKind.SubtractionSign)) {
                this.Advance(TokenKind.SubtractionSign);
                return this.ana.AnalyzeUnaryOperator(this.InvokeExpression(), "negate");
            }

            return this.InvokeExpression();
        }        

        private SyntaxPotential InvokeExpression() {
            IReadOnlyList<SyntaxPotential> getArgsList() {
                var args = new List<SyntaxPotential>();
                this.Advance(TokenKind.OpenParenthesis);

                while (!this.Peek(TokenKind.CloseParenthesis)) {
                    args.Add(this.Expression());

                    if (this.Peek(TokenKind.Comma)) {
                        this.Advance(TokenKind.Comma);
                    }
                }

                this.Advance(TokenKind.CloseParenthesis);
                return args;
            }

            var atom = this.Atom();

            while (this.Peek(TokenKind.OpenParenthesis) || this.Peek(TokenKind.Dot)) {
                if (this.Peek(TokenKind.OpenParenthesis)) {
                    var args = getArgsList();
                    atom = this.ana.AnalyzeInvoke(atom, args);
                }
                else {
                    this.Advance(TokenKind.Dot);
                    string name = this.Advance<string>();

                    if (this.Peek(TokenKind.OpenParenthesis)) {
                        var args = getArgsList();

                        atom = this.ana.AnalyzeInvoke(
                            this.ana.AnalyzeMemberAccess(atom, name),
                            new[] { atom }.Concat(args).ToArray()
                        );
                    }
                    else {
                        atom = this.ana.AnalyzeMemberAccess(atom, name);
                    }
                }
            }

            return atom;
        }

        private SyntaxPotential Atom() {
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
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.Block();
            }
            else if (this.Peek(TokenKind.EvokeKeyword)) {
                this.Advance(TokenKind.EvokeKeyword);
                return this.ana.AnalyzeEvoke();
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