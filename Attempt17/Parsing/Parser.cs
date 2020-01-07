using Attempt17.Features.FlowControl;
using Attempt17.Features.Functions;
using Attempt17.Features.Primitives;
using Attempt17.Features.Variables;
using Attempt17.Types;
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

        public ImmutableList<ISyntax<ParseTag>> Parse() {
            var list = ImmutableList<ISyntax<ParseTag>>.Empty;

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

        private ISyntax<ParseTag> FunctionDeclaration() {
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

            return new FunctionDeclarationParseSyntax(
                new ParseTag(loc), 
                sig, 
                body);
        }

        private ISyntax<ParseTag> Declaration() {
            if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionDeclaration();
            }

            throw ParsingErrors.UnexpectedToken(this.Advance());
        }

        private ISyntax<ParseTag> TopExpression() => this.StoreExpression();

        private ISyntax<ParseTag> StoreExpression() {
            var target = this.AddExpression();

            if (this.TryAdvance(TokenKind.LeftArrow)) {
                var value = this.AddExpression();
                var loc = target.Tag.Location.Span(value.Tag.Location);

                target = new StoreSyntax<ParseTag>(
                    new ParseTag(loc), 
                    target, 
                    value);
            }

            return target;
        }

        private ISyntax<ParseTag> AddExpression() {
            var first = this.InvokeExpression();

            while (true) {
                if (!this.Peek(TokenKind.AddSign) && !this.Peek(TokenKind.SubtractSign)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.AddSign ? BinarySyntaxKind.Add : BinarySyntaxKind.Subtract;
                var second = this.InvokeExpression();
                var loc = first.Tag.Location.Span(second.Tag.Location);

                first = new BinarySyntax<ParseTag>(
                    new ParseTag(loc), 
                    op, 
                    first, 
                    second);
            }

            return first;
        }

        private ISyntax<ParseTag> InvokeExpression() {
            var first = this.Atom();

            while (this.TryAdvance(TokenKind.OpenParenthesis)) {
                var args = ImmutableList<ISyntax<ParseTag>>.Empty;

                while (!this.Peek(TokenKind.CloseParenthesis)) {
                    args = args.Add(this.TopExpression());

                    if (!this.Peek(TokenKind.CloseParenthesis)) {
                        this.Advance(TokenKind.Comma);
                    }
                }

                var last = this.Advance(TokenKind.CloseParenthesis);
                var loc = first.Tag.Location.Span(last.Location);

                first = new InvokeParseSyntax(
                    new ParseTag(loc), 
                    first, 
                    args);
            }

            return first;
        }

        private ISyntax<ParseTag> VariableDeclarationStatement() {            
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
            var loc = tok.Location.Span(value.Tag.Location);

            return new VariableInitSyntax<ParseTag>(
                new ParseTag(loc),
                name,
                kind, 
                value);
        }

        private ISyntax<ParseTag> WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.DoKeyword);
            this.Advance(TokenKind.Colon);

            var body = this.TopExpression();
            var loc = start.Location.Span(body.Tag.Location);

            return new WhileSyntax<ParseTag>(
                new ParseTag(loc), 
                cond, 
                body);
        }

        public ISyntax<ParseTag> IfStatement() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();
            var next = this.Advance();

            if (next.Kind == TokenKind.ThenKeyword) {
                this.Advance(TokenKind.Colon);

                var affirm = this.TopExpression();

                this.Advance(TokenKind.ElseKeyword);
                this.Advance(TokenKind.Colon);

                var neg = this.TopExpression();
                var loc = start.Location.Span(neg.Tag.Location);

                return new IfSyntax<ParseTag>(
                    new ParseTag(loc), 
                    IfKind.Expression, 
                    cond, 
                    affirm, 
                    Option.Some(neg));
            }
            else if (next.Kind == TokenKind.DoKeyword) {
                this.Advance(TokenKind.Colon);

                var affirm = this.TopExpression();

                if (this.TryAdvance(TokenKind.ElseKeyword)) {
                    this.Advance(TokenKind.Colon);

                    var neg = this.TopExpression();
                    var loc = start.Location.Span(neg.Tag.Location);

                    return new IfSyntax<ParseTag>(
                        new ParseTag(loc), 
                        IfKind.Statement, 
                        cond, 
                        affirm, 
                        Option.Some(neg));
                }
                else {
                    var loc = start.Location.Span(affirm.Tag.Location);

                    return new IfSyntax<ParseTag>(
                        new ParseTag(loc), 
                        IfKind.Statement, 
                        cond, 
                        affirm, 
                        Option.None<ISyntax<ParseTag>>());
                }
            }
            else {
                throw ParsingErrors.UnexpectedToken(next);
            }
        }

        private ISyntax<ParseTag> Statement() {
            ISyntax<ParseTag> result;

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

        private ISyntax<ParseTag> IntLiteral() {
            var tok = (Token<long>)this.Advance(TokenKind.IntLiteral);

            return new IntLiteralSyntax<ParseTag>(
                new ParseTag(tok.Location), 
                tok.Value);
        }

        private ISyntax<ParseTag> BlockExpression() {
            var start = this.Advance(TokenKind.OpenBrace);
            var list = ImmutableList<ISyntax<ParseTag>>.Empty;

            while (!this.Peek(TokenKind.CloseBrace)) {
                list = list.Add(this.Statement());
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            return new BlockSyntax<ParseTag>(
                new ParseTag(loc), 
                list);
        }

        private ISyntax<ParseTag> VariableLiteral() {
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);

            return new VariableAccessParseSyntax(
                new ParseTag(tok.Location),
                VariableAccessKind.ValueAccess,
                tok.Value);
        }

        private ISyntax<ParseTag> VariableLiteralAccessLiteral() {
            var start = this.Advance(TokenKind.LiteralSign);
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);
            var loc = start.Location.Span(tok.Location);

            return new VariableAccessParseSyntax(
                new ParseTag(loc),
                VariableAccessKind.RemoteAccess,
                tok.Value);
        }

        private ISyntax<ParseTag> VoidLiteral() {
            var tok = this.Advance(TokenKind.VoidKeyword);

            return new VoidLiteralSyntax<ParseTag>(new ParseTag(tok.Location));
        }

        private ISyntax<ParseTag> Atom() {
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