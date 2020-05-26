using Attempt19.Features.Containers.Arrays;
using Attempt19.TypeChecking;
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

        public Syntax Parse() {
            return this.TopExpression();
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
                throw ParsingErrors.EndOfFile(this.tokens.Last().Location);
            }

            return this.tokens[this.pos++];
        }

        private IToken Advance(TokenKind kind) {
            var tok = this.Advance();

            if (tok.Kind != kind) {
                throw ParsingErrors.UnexpectedToken(kind, tok);
            }

            return tok;
        }

        private T Advance<T>() {
            var tok = this.Advance();

            if (!(tok is Token<T> ttok)) {
                throw ParsingErrors.UnexpectedToken(tok);
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
                throw new NotImplementedException();
            }
        }

        private FunctionSignature FunctionSignature() {
            this.Advance(TokenKind.FunctionKeyword);

            string funcName = this.Advance<string>();
            this.Advance(TokenKind.OpenParenthesis);

            var pars = ImmutableList<Parameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var parType = this.TypeExpression();
                var parName = this.Advance<string>();

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new Parameter(parName, parType));
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.YieldSign);

            var returnType = this.TypeExpression();
            var sig = new FunctionSignature(funcName, returnType, pars);

            return sig;
        }

        private Syntax TopExpression() => this.StoreExpression();

        private Syntax StoreExpression() {
            var target = this.OrExpression();

            while (this.TryAdvance(TokenKind.LeftArrow)) {
                var value = this.TopExpression();
                var loc = target.Data.AsParsedData().Location.Span(value.Data.AsParsedData().Location);

                target = SyntaxFactory.MakeStore(target, value, loc);
            }

            return target;
        }


        private Syntax OrExpression() {
            var first = this.XorExpression();

            while (this.Peek(TokenKind.OrKeyword)) {
                var second = this.XorExpression();
                var loc1 = first.Data.AsParsedData().Location;
                var loc2 = second.Data.AsParsedData().Location;

                first = SyntaxFactory.MakeBinaryExpression(
                    first, second, BinaryOperation.Or,
                    loc1.Span(loc2));
            }

            return first;
        }

        private Syntax XorExpression() {
            var first = this.ComparisonExpression();

            while (this.Peek(TokenKind.XorKeyword)) {
                var second = this.ComparisonExpression();
                var loc1 = first.Data.AsParsedData().Location;
                var loc2 = second.Data.AsParsedData().Location;

                first = SyntaxFactory.MakeBinaryExpression(
                    first, second, BinaryOperation.Xor,
                    loc1.Span(loc2));
            }

            return first;
        }

        private Syntax ComparisonExpression() {
            var first = this.AndExpression();
            var comparators = new Dictionary<TokenKind, BinaryOperation>() {
                { TokenKind.EqualSign, BinaryOperation.EqualTo }, { TokenKind.NotEqualSign, BinaryOperation.NotEqualTo },
                { TokenKind.LessThanSign, BinaryOperation.LessThan }, { TokenKind.GreaterThanSign, BinaryOperation.GreaterThan },
                { TokenKind.LessThanOrEqualToSign, BinaryOperation.LessThanOrEqualTo },
                { TokenKind.GreaterThanOrEqualToSign, BinaryOperation.GreaterThanOrEqualTo }
            };

            while (true) {
                bool worked = false;

                foreach (var (tok, _) in comparators) {
                    worked |= this.Peek(tok);
                }

                if (!worked) {
                    break;
                }

                var op = comparators[this.Advance().Kind];
                var second = this.AndExpression();
                var loc1 = first.Data.AsParsedData().Location;
                var loc2 = second.Data.AsParsedData().Location;

                first = SyntaxFactory.MakeBinaryExpression(
                    first, second, op, loc1.Span(loc2));
            }

            return first;
        }

        private Syntax AndExpression() {
            var first = this.AddExpression();

            while (this.Peek(TokenKind.AndKeyword)) {
                var second = this.AddExpression();
                var loc1 = first.Data.AsParsedData().Location;
                var loc2 = second.Data.AsParsedData().Location;

                first = SyntaxFactory.MakeBinaryExpression(
                    first, second, BinaryOperation.And, loc1.Span(loc2));
            }

            return first;
        }

        private Syntax AddExpression() {
            var first = this.MultiplyExpression();

            while (true) {
                if (!this.Peek(TokenKind.AddSign) && !this.Peek(TokenKind.SubtractSign)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.AddSign ? BinaryOperation.Add : BinaryOperation.Subtract;
                var second = this.MultiplyExpression();
                var loc1 = first.Data.AsParsedData().Location;
                var loc2 = second.Data.AsParsedData().Location;

                first = SyntaxFactory.MakeBinaryExpression(
                    first, second, op, loc1.Span(loc2));
            }

            return first;
        }

        private Syntax MultiplyExpression() {
            var first = this.Atom();

            while (true) {
                if (!this.Peek(TokenKind.MultiplySign)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.MultiplySign ? BinaryOperation.Multiply : BinaryOperation.Multiply;
                var second = this.Atom();
                var loc1 = first.Data.AsParsedData().Location;
                var loc2 = second.Data.AsParsedData().Location;

                first = SyntaxFactory.MakeBinaryExpression(
                    first, second, op, loc1.Span(loc2));
            }

            return first;
        }

        private Syntax VariableDeclarationStatement() {
            var tok = this.Advance(TokenKind.VarKeyword);
            var name = this.Advance<string>();

            this.Advance(TokenKind.LeftArrow);

            var value = this.TopExpression();
            var loc = tok.Location.Span(value.Data.AsParsedData().Location);

            return SyntaxFactory.MakeVariableInit(name, value, loc);
        }

        private Syntax Statement() {
            Syntax result;

            if (this.Peek(TokenKind.VarKeyword)) {
                result = this.VariableDeclarationStatement();
            }
            else {
                result = this.TopExpression();
            }

            this.Advance(TokenKind.Semicolon);

            return result;
        }

        private Syntax IntLiteral() {
            var tok = (Token<int>)this.Advance(TokenKind.IntLiteral);

            return SyntaxFactory.MakeIntLiteral(tok.Value, tok.Location);
        }

        private Syntax BlockExpression() {
            var start = this.Advance(TokenKind.OpenBrace);
            var list = new List<Syntax>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                list.Add(this.Statement());
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            return SyntaxFactory.MakeBlock(list, loc);
        }

        private Syntax VariableAccess() {
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);

            if (this.Peek(TokenKind.OpenBracket) || this.Peek(TokenKind.LiteralSign)) {
                ArrayAccessKind kind;

                if (this.Peek(TokenKind.OpenBracket)) {
                    this.Advance(TokenKind.OpenBracket);
                    kind = ArrayAccessKind.ValueAccess;
                }
                else if (this.Peek(TokenKind.LiteralSign)) {
                    this.Advance(TokenKind.LiteralSign);
                    this.Advance(TokenKind.OpenBracket);
                    kind = ArrayAccessKind.LiteralAccess;
                }
                else {
                    throw new Exception("This isn't supposed to happen");
                }

                var inner = this.TopExpression();
                var last = this.Advance(TokenKind.CloseBracket);
                var loc = tok.Location.Span(last.Location);

                return SyntaxFactory.MakeArrayAccess(tok.Value, inner, kind, loc);
            }

            return SyntaxFactory.MakeVariableAccess(tok.Value, tok.Location);
        }

        private Syntax VoidLiteral() {
            var tok = this.Advance(TokenKind.VoidKeyword);

            return SyntaxFactory.MakeVoidLiteral(tok.Location);
        }

        private Syntax ParenGroup() {
            this.Advance(TokenKind.OpenParenthesis);

            var result = this.TopExpression();

            this.Advance(TokenKind.CloseParenthesis);

            return result;
        }

        private Syntax BoolLiteral() {
            var start = (Token<bool>)this.Advance(TokenKind.BoolLiteral);

            return SyntaxFactory.MakeBoolLiteral(start.Value, start.Location);
        }

        private Syntax VariableLiteral() {
            var start = this.Advance(TokenKind.LiteralSign);
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);
            var loc = start.Location.Span(tok.Location);

            return SyntaxFactory.MakeVariableLiteral(tok.Value, loc);
        }

        private Syntax MoveExpression() {
            var start = this.Advance(TokenKind.MoveKeyword);
            var varTok = (Token<string>)this.Advance(TokenKind.Identifier);
            var loc = start.Location.Span(varTok.Location);

            return SyntaxFactory.MakeVariableMove(varTok.Value, loc);
        }

        private Syntax ArrayLiteral() {
            var start = this.Advance(TokenKind.OpenBracket);
            var elems = new List<Syntax>();

            while (!this.Peek(TokenKind.CloseBracket)) {
                elems.Add(this.TopExpression());

                if (!this.Peek(TokenKind.CloseBracket)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Location.Span(end.Location);

            return SyntaxFactory.MakeArrayLiteral(elems, loc);
        }

        private Syntax Atom() {
            if (this.Peek(TokenKind.LiteralSign)) {
                return this.VariableLiteral();
            }
            else if (this.Peek(TokenKind.IntLiteral)) {
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
            else if (this.Peek(TokenKind.MoveKeyword)) {
                return this.MoveExpression();
            }
            else if (this.Peek(TokenKind.OpenBracket)) {
                return this.ArrayLiteral();
            }
            else {
                var next = this.Advance();

                throw ParsingErrors.UnexpectedToken(next);
            }
        }
    }
}