using Attempt17.Features;
using Attempt17.Features.BinaryExpression;
using Attempt17.Features.Block;
using Attempt17.Features.FlowControl;
using Attempt17.Features.Functions;
using Attempt17.Features.IntLiteral;
using Attempt17.Features.Variables;
using Attempt17.Features.VoidLiteral;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt17.Parsing {
    public class Parser {
        private int pos = 0;
        private readonly IReadOnlyList<IToken> tokens;

        public Parser(IReadOnlyList<IToken> tokens) {
            this.tokens = tokens;
        }

        public ImmutableList<IDeclarationParseTree> Parse() {
            var list = ImmutableList<IDeclarationParseTree>.Empty;

            while (this.pos < tokens.Count) {
                list = list.Add(this.Declaration());
            }

            return list;
        }

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

        private LanguageType TypeExpression() {
            if (this.Peek(TokenKind.VarKeyword)) {
                return this.VarTypeExpression();
            }

            return this.TypeAtom();
        }

        private LanguageType VarTypeExpression() {
            this.Advance(TokenKind.VarKeyword);
            var inner = this.TypeExpression();

            return new VariableType(inner);
        }

        private LanguageType TypeAtom() {
            if (this.TryAdvance(TokenKind.IntKeyword)) {
                return IntType.Instance;
            }
            else if (this.TryAdvance(TokenKind.VoidKeyword)) {
                return VoidType.Instance;
            }
            else {
                var path = new IdentifierPath(this.Advance<string>());
                return new NamedType(path);
            }
        }

        private IDeclarationParseTree FunctionDeclaration() {
            var start = this.Advance(TokenKind.FunctionKeyword);

            string funcName = this.Advance<string>();
            this.Advance(TokenKind.OpenParenthesis);

            var pars = ImmutableList<FunctionParameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var parType = this.TypeExpression();
                var parName = this.Advance<string>();

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new FunctionParameter(parName, parType));
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.YieldSign);

            var returnType = this.TypeExpression();
            var end = this.Advance(TokenKind.Colon);

            var sig = new FunctionSignature(funcName, returnType, pars);
            var body = this.TopExpression();
            var loc = start.Location.Span(end.Location);

            this.Advance(TokenKind.Semicolon);

            return new FunctionDeclarationParseTree(loc, sig, body);
        }

        private IDeclarationParseTree Declaration() {
            if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionDeclaration();
            }

            throw ParsingErrors.UnexpectedToken(this.Advance());
        }

        private IParseTree TopExpression() => this.StoreExpression();

        private IParseTree StoreExpression() {
            var target = this.AddExpression();

            if (this.TryAdvance(TokenKind.LeftArrow)) {
                var value = this.AddExpression();
                var loc = target.Location.Span(value.Location);

                target = new VariableStoreParseTree(loc, target, value);
            }

            return target;
        }

        private IParseTree AddExpression() {
            var first = this.InvokeExpression();

            while (true) {
                if (!this.Peek(TokenKind.AddSign) && !this.Peek(TokenKind.SubtractSign)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.AddSign ? BinaryExpressionKind.Add : BinaryExpressionKind.Subtract;
                var second = this.InvokeExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinaryExpressionParseTree(loc, op, first, second);
            }

            return first;
        }

        private IParseTree InvokeExpression() {
            var first = this.Atom();

            while (this.TryAdvance(TokenKind.OpenParenthesis)) {
                var args = ImmutableList<IParseTree>.Empty;

                while (!this.Peek(TokenKind.CloseParenthesis)) {
                    args = args.Add(this.TopExpression());

                    if (!this.Peek(TokenKind.CloseParenthesis)) {
                        this.Advance(TokenKind.Comma);
                    }
                }

                var last = this.Advance(TokenKind.CloseParenthesis);
                var loc = first.Location.Span(last.Location);

                first = new InvokeParseTree(loc, first, args);
            }

            return first;
        }

        private IParseTree VariableDeclarationStatement() {            
            var tok = this.Advance(TokenKind.VarKeyword);
            var name = this.Advance<string>();
            var op = this.Advance();

            VariableInitKind kind;

            if (op.Kind == TokenKind.LeftArrow) {
                kind = VariableInitKind.Store;
            }
            else if (op.Kind == TokenKind.EqualSign) {
                kind = VariableInitKind.Equate;
            }
            else {
                throw ParsingErrors.UnexpectedToken(op);
            }

            var value = this.TopExpression();
            var loc = tok.Location.Span(value.Location);

            return new VariableInitParseTree(kind, loc, name, value);
        }

        private IParseTree WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.DoKeyword);
            this.Advance(TokenKind.Colon);

            var body = this.TopExpression();
            var loc = start.Location.Span(body.Location);

            return new WhileParseTree(loc, cond, body);
        }

        public IParseTree IfStatement() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();
            var next = this.Advance();

            if (next.Kind == TokenKind.ThenKeyword) {
                this.Advance(TokenKind.Colon);

                var affirm = this.TopExpression();

                this.Advance(TokenKind.ElseKeyword);
                this.Advance(TokenKind.Colon);

                var neg = this.TopExpression();
                var loc = start.Location.Span(neg.Location);

                return new IfParseTree(loc, IfKind.Expression, cond, affirm, Option.Some(neg));
            }
            else if (next.Kind == TokenKind.DoKeyword) {
                this.Advance(TokenKind.Colon);

                var affirm = this.TopExpression();

                if (this.TryAdvance(TokenKind.ElseKeyword)) {
                    this.Advance(TokenKind.Colon);

                    var neg = this.TopExpression();
                    var loc = start.Location.Span(neg.Location);

                    return new IfParseTree(loc, IfKind.Statement, cond, affirm, Option.Some(neg));
                }
                else {
                    var loc = start.Location.Span(affirm.Location);

                    return new IfParseTree(loc, IfKind.Statement, cond, affirm, Option.None<IParseTree>());
                }
            }
            else {
                throw ParsingErrors.UnexpectedToken(next);
            }
        }

        private IParseTree Statement() {
            IParseTree result;

            if (this.Peek(TokenKind.VarKeyword)) {
                result = this.VariableDeclarationStatement();
            }
            else if (this.Peek(TokenKind.IfKeyword)) {
                result = this.IfStatement();
            }
            else if (this.Peek(TokenKind.WhileKeyword)) {
                result = this.WhileStatement();
            }
            else {
                result = this.TopExpression();
            }

            this.Advance(TokenKind.Semicolon);

            return result;
        }

        private IParseTree IntLiteral() {
            var tok = (Token<long>)this.Advance(TokenKind.IntLiteral);

            return new IntLiteralParseTree(tok.Location, tok.Value);
        }

        private IParseTree BlockExpression() {
            var start = this.Advance(TokenKind.OpenBrace);
            var list = ImmutableList<IParseTree>.Empty;

            while (!this.Peek(TokenKind.CloseBrace)) {
                list = list.Add(this.Statement());
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            return new BlockParseTree(loc, list);
        }

        private IParseTree VariableLiteral() {
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);

            return new VariableLiteralParseTree(tok.Location, tok.Value, VariableLiteralKind.ValueAccess);
        }

        private IParseTree VariableLiteralAccessLiteral() {
            var start = this.Advance(TokenKind.LiteralSign);
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);
            var loc = start.Location.Span(tok.Location);

            return new VariableLiteralParseTree(loc, tok.Value, VariableLiteralKind.LiteralAccess);
        }

        private IParseTree VoidLiteral() {
            var tok = this.Advance(TokenKind.VoidKeyword);

            return new VoidLiteralTree(tok.Location);
        }

        private IParseTree Atom() {
            if (this.Peek(TokenKind.IntLiteral)) {
                return this.IntLiteral();
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.BlockExpression();
            }
            else if (this.Peek(TokenKind.Identifier)) {
                return this.VariableLiteral();
            }
            else if (this.Peek(TokenKind.LiteralSign)) {
                return this.VariableLiteralAccessLiteral();
            }
            else if (this.Peek(TokenKind.VoidKeyword)) {
                return this.VoidLiteral();
            }
            else {
                var next = this.Advance();

                throw ParsingErrors.UnexpectedToken(next);
            }
        }
    }
}