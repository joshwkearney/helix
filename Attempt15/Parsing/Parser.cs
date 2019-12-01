using JoshuaKearney.Attempt15.Syntax;
using JoshuaKearney.Attempt15.Syntax.Arithmetic;
using JoshuaKearney.Attempt15.Syntax.Conditionals;
using JoshuaKearney.Attempt15.Syntax.Functions;
using JoshuaKearney.Attempt15.Syntax.Logic;
using JoshuaKearney.Attempt15.Syntax.Tuples;
using JoshuaKearney.Attempt15.Syntax.Variables;
using JoshuaKearney.Attempt15.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JoshuaKearney.Attempt15.Parsing {
    public delegate ITrophyType TypePotential(Scope context);

    public class Parser {
        private readonly Lexer lexer;
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

        private TypePotential TypeExpression() {
            return this.TypeFunctionExpression();
        }

        private TypePotential TypeFunctionExpression() {
            var args = new List<TypePotential> { this.TypeAtom() };

            while (this.Peek(TokenKind.MultiplicationSign)) {
                this.Advance(TokenKind.MultiplicationSign);
                args.Add(this.TypeAtom());
            }

            if (!this.Peek(TokenKind.Arrow) && args.Count == 1) {
                return args[0];
            }
            else if (!this.Peek(TokenKind.Arrow)) {
                throw new Exception();
            }
            else {
                this.Advance(TokenKind.Arrow);
                var ret = this.TypeAtom();

                return scope => {
                    return new FunctionInterfaceType(
                        ret(scope),
                        args.Select(x => x(scope))
                    );
                };
            }
        }

        private TypePotential TypeAtom() {
            string type = this.Advance<string>();

            if (type == "int") {
                return _ => new SimpleType(TrophyTypeKind.Int);
            }
            else if (type == "float") {
                return _ => new SimpleType(TrophyTypeKind.Float);
            }
            else if (type == "bool") {
                return _ => new SimpleType(TrophyTypeKind.Boolean);
            }
            else {
                return scope => scope.TypeDeclarations[type];
            }
        }
        
        private IParseTree Expression() {           
            if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionExpression();
            }

            return this.OrExpression();
        }

        private IParseTree BlockExpression() {
            if (this.Peek(TokenKind.IfKeyword)) {
                return this.IfExpression();
            }
            else if (this.Peek(TokenKind.LetKeyword) || this.Peek(TokenKind.VarKeyword)) {
                return this.LetExpression();
            }
            else if (this.Peek(TokenKind.TypeKeyword)) {
                return this.TypeDeclaration();
            }
            else if (this.Peek(TokenKind.SetKeyword)) {
                return this.AssignExpression();
            }

            return this.Expression();
        }

        private IParseTree EvokeExpression() {
            this.Advance(TokenKind.EvokeKeyword);

            this.Advance(TokenKind.OpenParenthesis);
            var list = new List<IParseTree>();

            while (true) {
                if (this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.CloseParenthesis);
                    break;
                }

                list.Add(this.Expression());

                if (this.Peek(TokenKind.Comma)) {
                    this.Advance(TokenKind.Comma);
                }
                else {
                    this.Advance(TokenKind.CloseParenthesis);
                    break;
                }
            }

            return new EvokeParseTree(list);
        }

        private IParseTree FunctionExpression() {
            this.Advance(TokenKind.FunctionKeyword);

            TypePotential returnType = _ => null;
            if (!this.Peek(TokenKind.OpenParenthesis)) {
                returnType = this.TypeExpression();
            }

            this.Advance(TokenKind.OpenParenthesis);

            var pars = new List<ParseFunctionParameter>();
            while (true) {
                if (this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.CloseParenthesis);
                    break;
                }

                var type = this.TypeExpression();
                var name = this.Advance<string>();

                pars.Add(new ParseFunctionParameter(name, type));

                if (this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.CloseParenthesis);
                    break;
                }
                else {
                    this.Advance(TokenKind.Comma);
                }
            }

            this.Advance(TokenKind.Arrow);
            var body = this.Expression();

            return new FunctionLiteralParseTree(body, pars, returnType);
        }

        private IParseTree AssignExpression() {
            this.Advance(TokenKind.SetKeyword);
            string name = this.Advance<string>();

            this.Advance(TokenKind.ToKeyword);
            var assign = this.Expression();
            var appendix = this.BlockExpression();

            return new VariableAssignmentParseTree(name, assign, appendix);
        }

        private IParseTree IfExpression() {
            this.Advance(TokenKind.IfKeyword);
            var cond = this.Expression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.Expression();

            this.Advance(TokenKind.ElseKeyword);
            var neg = this.Expression();

            return new IfParseTree(cond, affirm, neg);
        }

        private IParseTree LetExpression() {
            bool isImmutable;

            if (this.Peek(TokenKind.LetKeyword)) {
                this.Advance(TokenKind.LetKeyword);
                isImmutable = true;
            }
            else {
                this.Advance(TokenKind.VarKeyword);
                isImmutable = false;
            }

            string name = this.Advance<string>();

            this.Advance(TokenKind.EqualsSign);
            var assign = this.Expression();

            var appendix = this.BlockExpression();

            return new VariableDeclarationParseTree(name, assign, isImmutable, appendix);
        }

        private IParseTree TypeDeclaration() {
            this.Advance(TokenKind.TypeKeyword);
            string name = this.Advance<string>();

            this.Advance(TokenKind.EqualsSign);
            var type = this.TypeExpression();

            this.Advance(TokenKind.Semicolon);
            var appendix = this.BlockExpression();

            return new TypeVariableDeclarationParseTree(name, type, appendix);
        }

        private IParseTree TupleExpression() {
            this.Advance(TokenKind.Pipe);

            var values = new List<ParseTupleMember>();
            int counter = 1;

            while (true) {
                if (this.Peek(TokenKind.Identifier)) {
                    string id = this.Advance<string>();

                    if (this.Peek(TokenKind.Colon)) {
                        this.Advance(TokenKind.Colon);
                        values.Add(new ParseTupleMember(id, this.Expression()));
                    }
                    else {
                        values.Add(new ParseTupleMember(
                            "item" + counter,
                            new VariableLiteralParseTree(id)
                        ));
                    }
                }
                else {
                    values.Add(new ParseTupleMember(
                        "item" + counter,
                        this.Expression()
                    ));
                }

                counter++;

                if (this.Peek(TokenKind.Comma)) {
                    this.Advance(TokenKind.Comma);
                }
                else if (this.Peek(TokenKind.Pipe)) {
                    this.Advance(TokenKind.Pipe);
                    break;
                }
            }

            return new TupleLiteralParseTree(values);
        }

        private IParseTree OrExpression() {
            var first = this.XorExpression();

            while (this.Peek(TokenKind.OrKeyword)) {
                this.Advance(TokenKind.OrKeyword);

                first = new BooleanBinaryParseTree(first, this.XorExpression(), BooleanBinaryOperationKind.Or);
            }

            return first;
        }

        private IParseTree XorExpression() {
            var first = this.AndExpression();

            while (this.Peek(TokenKind.XorKeyword)) {
                this.Advance(TokenKind.XorKeyword);

                first = new BooleanBinaryParseTree(first, this.AndExpression(), BooleanBinaryOperationKind.Xor);
            }

            return first;
        }

        private IParseTree AndExpression() {
            var first = this.ComparisonExpression();

            while (this.Peek(TokenKind.AndKeyword)) {
                this.Advance(TokenKind.AndKeyword);

                first = new BooleanBinaryParseTree(first, this.ComparisonExpression(), BooleanBinaryOperationKind.And);
            }

            return first;
        }

        private IParseTree ComparisonExpression() {
            var first = this.AddExpression();

            bool hasNext() => this.Peek(TokenKind.LessThanSign)
                || this.Peek(TokenKind.GreaterThanSign)
                || this.Peek(TokenKind.EqualsSign)
                || this.Peek(TokenKind.SpaceshipSign);

            while (hasNext()) {
                if (this.Peek(TokenKind.GreaterThanSign)) {
                    this.Advance(TokenKind.GreaterThanSign);
                    first = new ArithmeticBinaryParseTree(first, this.AddExpression(), ArithmeticBinaryOperationKind.GreaterThan);
                }
                else if (this.Peek(TokenKind.LessThanSign)) {
                    this.Advance(TokenKind.LessThanSign);
                    first = new ArithmeticBinaryParseTree(first, this.AddExpression(), ArithmeticBinaryOperationKind.LessThan);
                }
                else if (this.Peek(TokenKind.EqualsSign)) {
                    this.Advance(TokenKind.EqualsSign);
                    first = new ArithmeticBinaryParseTree(first, this.AddExpression(), ArithmeticBinaryOperationKind.EqualTo);
                }
                else if (this.Peek(TokenKind.SpaceshipSign)) {
                    this.Advance(TokenKind.SpaceshipSign);
                    first = new ArithmeticBinaryParseTree(first, this.AddExpression(), ArithmeticBinaryOperationKind.Spaceship);
                }
                else {
                    throw new Exception();
                }
            }

            return first;
        }

        private IParseTree AddExpression() {
            var mult = this.MultiplyExpression();

            while (this.Peek(TokenKind.AdditionSign) || this.Peek(TokenKind.SubtractionSign)) {
                if (this.Advance().Kind == TokenKind.AdditionSign) {
                    mult = new ArithmeticBinaryParseTree(mult, this.MultiplyExpression(), ArithmeticBinaryOperationKind.Addition);
                }
                else {
                    mult = new ArithmeticBinaryParseTree(mult, this.MultiplyExpression(), ArithmeticBinaryOperationKind.Subtraction);
                }
            }

            return mult;
        }

        private IParseTree MultiplyExpression() {
            var invoke = this.ExponentExpression();

            while (this.Peek(TokenKind.MultiplicationSign) || this.Peek(TokenKind.DivisionSign) || this.Peek(TokenKind.StrictDivisionSign)) {
                var advance = this.Advance().Kind;

                if (advance == TokenKind.MultiplicationSign) {
                    invoke = new ArithmeticBinaryParseTree(invoke, this.ExponentExpression(), ArithmeticBinaryOperationKind.Multiplication);
                }
                else if (advance == TokenKind.DivisionSign) {
                    invoke = new ArithmeticBinaryParseTree(invoke, this.ExponentExpression(), ArithmeticBinaryOperationKind.Division);
                }
                else {
                    invoke = new ArithmeticBinaryParseTree(invoke, this.ExponentExpression(), ArithmeticBinaryOperationKind.StrictDivision);
                }
            }

            return invoke;
        }

        private IParseTree ExponentExpression() {
            var invoke = this.InvokeExpression();

            while (this.Peek(TokenKind.ExponentSign)) {
                this.Advance(TokenKind.ExponentSign);
                invoke = new ArithmeticBinaryParseTree(invoke, this.InvokeExpression(), ArithmeticBinaryOperationKind.Exponentiation);
            }

            return invoke;
        }

        private IParseTree InvokeExpression() {
            var first = this.Atom();

            while (this.Peek(TokenKind.OpenParenthesis)) {
                var list = new List<IParseTree>();

                this.Advance(TokenKind.OpenParenthesis);
                while (true) {
                    if (this.Peek(TokenKind.CloseParenthesis)) {
                        this.Advance(TokenKind.CloseParenthesis);
                        break;
                    }

                    list.Add(this.Expression());

                    if (this.Peek(TokenKind.Comma)) {
                        this.Advance(TokenKind.Comma);
                    }
                    else {
                        this.Advance(TokenKind.CloseParenthesis);
                        break;
                    }
                }

                first = new FunctionCallParseTree(first, list);
            }

            return first;
        }

        private IParseTree Atom() {
            if (this.Peek() is Token<long> intTok) {
                this.Advance();
                return new IntLiteralTree(intTok.Value);
            }
            else if (this.Peek() is Token<double> realTok) {
                this.Advance();
                return new RealLiteralTree(realTok.Value);
            }
            else if (this.Peek() is Token<bool> boolTok) {
                this.Advance();
                return new BooleanLiteralTree(boolTok.Value);
            }
            else if (this.Peek(TokenKind.OpenParenthesis)) {
                this.Advance(TokenKind.OpenParenthesis);
                var result = this.Expression();
                this.Advance(TokenKind.CloseParenthesis);

                return result;
            }
            else if (this.Peek(TokenKind.Identifier) && this.Peek() is Token<string> strTok) {
                this.Advance(TokenKind.Identifier);
                return new VariableLiteralParseTree(strTok.Value);
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.OpenBrace);
                var result = this.BlockExpression();
                this.Advance(TokenKind.CloseBrace);

                return result;
            }
            else if (this.Peek(TokenKind.EvokeKeyword)) {
                return this.EvokeExpression();
            }
            else if (this.Peek(TokenKind.Pipe)) {
                return this.TupleExpression();
            }
            else {
                throw new Exception();
            }
        }
    }
}