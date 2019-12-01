using Attempt12.Analyzing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt12 {
    public class Parser {
        private Lexer lexer;
        private int pos = 0;
        private IReadOnlyList<Token> tokens;

        public Parser(Lexer lexer) {
            this.lexer = lexer;
        }

        public Analyzable Parse() {
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
                throw new Exception($"Expected '{typeof(T).Name}', got '{tok}'");
            }
        }

        private TypeResolvable TypeExpression() {
            return this.FunctionTypeExpression();
        }

        private TypeResolvable FunctionTypeExpression() {
            var first = this.TypeProduct();

            while (this.Peek(TokenKind.Arrow)) {
                this.Advance(TokenKind.Arrow);

                var param = first;
                var ret = this.TypeProduct();

                first = x => x.ResolveFunctionType(param, ret);
            }

            return first;
        }

        private TypeResolvable TypeProduct() {
            List<TypeResolvable> types = new List<TypeResolvable> {
                this.TypeAtom()
            };

            while (this.Peek(TokenKind.MultiplicationSign)) {
                this.Advance(TokenKind.MultiplicationSign);
                types.Add(this.TypeAtom());
            }

            if (types.Count == 1) {
                return types.First();
            }
            else {
                return x => x.ResolveProductType(types);
            }
        }

        private TypeResolvable TypeAtom() {
            if (this.Peek() is Token<string> id) {
                this.Advance<string>();
                return x => x.ResolveTypeIdentifier(id.Value);
            }
            else if (this.Peek(TokenKind.OpenParenthesis)) {
                this.Advance(TokenKind.OpenParenthesis);
                var result = this.TypeExpression();
                this.Advance(TokenKind.CloseParenthesis);

                return result;
            }
            else {
                throw new Exception();
            }
        }

        private Analyzable Expression() {
            if (this.Peek(TokenKind.LetKeyword)) {
                return this.LetExpression();
            }
            else if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionExpression();
            }

            return this.AddExpression();
        }

        private Analyzable LetExpression() {
            this.Advance(TokenKind.LetKeyword);

            string name = this.Advance<string>();
            this.Advance(TokenKind.EqualsSign);

            var assign = this.Expression();

            return x => x.AnalyzeVariableDeclaration(name, assign);
        }

        private Analyzable FunctionExpression() {
            this.Advance(TokenKind.FunctionKeyword);
            this.Advance(TokenKind.OpenParenthesis);

            List<(TypeResolvable type, string name)> pars = new List<(TypeResolvable type, string name)>();
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var type = this.TypeExpression();
                string name = this.Advance<string>();

                pars.Add((type, name));

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.Arrow);

            var body = this.Expression();
            return x => x.AnalyzeFunctionDefinition(pars, body);
        }

        private Analyzable Block() {
            this.Advance(TokenKind.OpenBrace);

            Analyzable BlockExpression() {
                var first = this.Expression();

                if (this.Peek(TokenKind.Semicolon)) {
                    this.Advance(TokenKind.Semicolon);

                    if (this.Peek(TokenKind.CloseBrace)) {
                        this.Advance(TokenKind.CloseBrace);
                        return first;
                    }
                    else {
                        var next = BlockExpression();
                        return x => x.AnalyzeStatement(first, next);
                    }
                }
                else if (this.Peek(TokenKind.CloseBrace)) {
                    this.Advance(TokenKind.CloseBrace);
                    return first;
                }
                else {
                    var next = BlockExpression();
                    return x => x.AnalyzeStatement(first, next);
                }
            }

            return BlockExpression();
        }

        private Analyzable AddExpression() {
            var mult = this.MultiplyExpression();

            while (this.Peek(TokenKind.AdditionSign) || this.Peek(TokenKind.SubtractionSign)) {
                var op = this.Advance().Kind == TokenKind.AdditionSign
                    ? BinaryOperator.Add
                    : BinaryOperator.Subtract;

                var first = mult;
                var next = this.MultiplyExpression();

                mult = x => x.AnalyzeBinaryExpression(first, next, op);
            }

            return mult;
        }

        private Analyzable MultiplyExpression() {
            var invoke = this.InvokeExpression();

            while (this.Peek(TokenKind.MultiplicationSign) || this.Peek(TokenKind.DivisionSign)) {
                var op = this.Advance().Kind == TokenKind.MultiplicationSign
                    ? BinaryOperator.Multiply
                    : BinaryOperator.Divide;

                var first = invoke;
                var next = this.InvokeExpression();

                invoke = x => x.AnalyzeBinaryExpression(first, next, op);
            }

            return invoke;
        }

        private Analyzable InvokeExpression() {
            var atom = this.Atom();

            while (this.Peek(TokenKind.OpenParenthesis)) {
                this.Advance(TokenKind.OpenParenthesis);

                List<Analyzable> args = new List<Analyzable>();
                while (!this.Peek(TokenKind.CloseParenthesis)) {
                    args.Add(this.Expression());

                    if (!this.Peek(TokenKind.CloseParenthesis)) {
                        this.Advance(TokenKind.Comma);
                    }
                }

                this.Advance(TokenKind.CloseParenthesis);

                var func = atom;
                atom = x => x.AnalyzeInvoke(func, args);
            }

            return atom;
        }

        private Analyzable Atom() {
            if (this.Peek() is Token<int> intTok) {
                this.Advance();
                return x => x.AnalyzeInt32(intTok.Value);
            }
            if (this.Peek() is Token<float> realTok) {
                this.Advance();
                return x => x.AnalyzeReal32(realTok.Value);
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
                return x => x.AnalyzeVariableReference(name);
            }
            else {
                throw new Exception();
            }
        }
    }
}