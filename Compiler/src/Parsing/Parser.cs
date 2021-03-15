using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.Containers;
using Trophy.Features.Containers.Arrays;
using Trophy.Features.Containers.Structs;
using Trophy.Features.FlowControl;
using Trophy.Features.Functions;
using Trophy.Features.Primitives;
using Trophy.Features.Variables;

namespace Trophy.Parsing {
    public class Parser {
        private int pos = 0;
        private readonly IReadOnlyList<IToken> tokens;

        public Parser(IReadOnlyList<IToken> tokens) {
            this.tokens = tokens;
        }

        public IReadOnlyList<IDeclarationA> Parse() {
            var list = new List<IDeclarationA>();

            while (pos < tokens.Count) {
                list.Add(this.Declaration());
            }

            return list;
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

            if (tok is not Token<T> ttok) {
                throw ParsingErrors.UnexpectedToken(tok);
            }

            return ttok.Value;
        }

        /** Type Parsing **/
        private ITrophyType VarTypeExpression() {
            if (this.TryAdvance(TokenKind.VarKeyword)) {
                var inner = this.TypeExpression();

                return new VarRefType(inner, false);
            }
            else {
                this.Advance(TokenKind.RefKeyword);
                var inner = this.TypeExpression();

                return new VarRefType(inner, true);
            }
        }

        private ITrophyType TypeExpression() {
            if (this.Peek(TokenKind.VarKeyword) || this.Peek(TokenKind.RefKeyword)) {
                return this.VarTypeExpression();
            }

            return this.TypeAtom();
        }

        private ITrophyType TypeAtom() {
            if (this.TryAdvance(TokenKind.IntKeyword)) {
                return ITrophyType.Integer;
            }
            else if (this.TryAdvance(TokenKind.VoidKeyword)) {
                return ITrophyType.Void;
            }
            else if (this.TryAdvance(TokenKind.BoolKeyword)) {
                return ITrophyType.Boolean;
            }
            else if (this.TryAdvance(TokenKind.ArrayKeyword)) {
                this.Advance(TokenKind.OpenBracket);
                bool isreadonly = false;

                if (!this.TryAdvance(TokenKind.VarKeyword)) {
                    this.Advance(TokenKind.RefKeyword);
                    isreadonly = true;
                }

                var inner = this.TypeExpression();

                if (this.TryAdvance(TokenKind.Comma)) {
                    var count = this.Advance<int>();
                    this.Advance(TokenKind.CloseBracket);

                    return new FixedArrayType(inner, count, isreadonly);
                }
                else {
                    this.Advance(TokenKind.CloseBracket);

                    return new ArrayType(inner, isreadonly);
                }
            }
            else if (this.TryAdvance(TokenKind.FunctionKeyword)) {
                this.Advance(TokenKind.OpenBracket);
                var args = new List<ITrophyType>();

                while (!this.TryAdvance(TokenKind.YieldSign)) {
                    args.Add(this.TypeExpression());

                    if (!this.TryAdvance(TokenKind.Comma)) {
                        break;
                    }
                }

                var returnType = this.TypeAtom();
                this.Advance(TokenKind.CloseBracket);

                return new FunctionType(returnType, args);
            }
            else {
                return new NamedType(new IdentifierPath(this.Advance<string>()));
            }
        }

        /** Lifetime Parsing **/
        private string Lifetime() {
            if (this.TryAdvance(TokenKind.StackKeyword)) {
                return "stack";
            }
            else if (this.TryAdvance(TokenKind.HeapKeyword)) {
                return "heap";
            }
            else {
                return this.Advance<string>();
            }
        }

        /** Declaration Parsing **/
        private FunctionSignature FunctionSignature() {
            this.Advance(TokenKind.FunctionKeyword);
            var funcName = this.Advance<string>();

            this.Advance(TokenKind.OpenParenthesis);

            var pars = ImmutableList<FunctionParameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var parName = this.Advance<string>();
                this.Advance(TokenKind.AsKeyword);
                var parType = this.TypeExpression();

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new FunctionParameter(parName, parType));
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.AsKeyword);            
            
            var returnType = this.TypeExpression();
            var sig = new FunctionSignature(funcName, returnType, pars);

            return sig;
        }

        private IDeclarationA FunctionDeclaration() {
            var start = this.tokens[this.pos];
            var sig = this.FunctionSignature();
            var end = this.Advance(TokenKind.YieldSign);

            var body = this.TopExpression();
            var loc = start.Location.Span(end.Location);

            this.Advance(TokenKind.Semicolon);

            return new FunctionDeclarationA(loc, sig, body);
        }

        private IDeclarationA AggregateDeclaration() {
            IToken start;
            if (this.Peek(TokenKind.StructKeyword)) {
                start = this.Advance(TokenKind.StructKeyword);
            }
            else {
                start = this.Advance(TokenKind.UnionKeyword);
            }

            var name = this.Advance<string>();
            var mems = new List<StructMember>();
            var decls = new List<IDeclarationA>();
            var generics = new List<string>();

            if (this.TryAdvance(TokenKind.OpenBracket)) {
                while (true) {
                    generics.Add(this.Advance<string>());

                    if (!this.TryAdvance(TokenKind.Comma)) {
                        break;
                    }
                }

                this.Advance(TokenKind.CloseBracket);
            }

            this.Advance(TokenKind.OpenBrace);

            while (!this.Peek(TokenKind.FunctionKeyword) && !this.Peek(TokenKind.StructKeyword) && !this.Peek(TokenKind.CloseBrace)) {
                var memName = this.Advance<string>();
                this.Advance(TokenKind.AsKeyword);
                var memType = this.TypeExpression();

                this.Advance(TokenKind.Semicolon);
                mems.Add(new StructMember(memName, memType));
            }

            while (!this.Peek(TokenKind.CloseBrace)) {
                decls.Add(this.Declaration());
            }

            this.Advance(TokenKind.CloseBrace);
            var last = this.Advance(TokenKind.Semicolon);
            var loc = start.Location.Span(last.Location);
            var sig = new AggregateSignature(name, mems);
            var kind = start.Kind == TokenKind.StructKeyword ? AggregateKind.Struct : AggregateKind.Union;

            var result = new AggregateDeclarationA(loc, sig, kind, decls);

            if (generics.Any()) {
                return new MetaStructDeclarationA(result, generics);
            }
            else {
                return result;
            }
        }

        private IDeclarationA Declaration() {
            if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionDeclaration();
            }
            else if (this.Peek(TokenKind.StructKeyword) || this.Peek(TokenKind.UnionKeyword)) {
                return this.AggregateDeclaration();
            }

            throw ParsingErrors.UnexpectedToken(this.Advance());
        }

        /** Expression Parsing **/
        private ISyntaxA TopExpression() => this.AsExpression();

        private ISyntaxA AsExpression() {
            var first = this.OrExpression();

            while (this.TryAdvance(TokenKind.AsKeyword)) {
                var target = this.TypeExpression();

                first = new AsSyntaxA(
                    first.Location.Span(this.tokens[this.pos - 1].Location),
                    first,
                    target);
            }

            return first;
        }

        private ISyntaxA OrExpression() {
            var first = this.XorExpression();

            while (this.TryAdvance(TokenKind.OrKeyword)) {
                var second = this.XorExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntaxA(loc, first, second, BinaryOperation.Or);
            }

            return first;
        }

        private ISyntaxA XorExpression() {
            var first = this.ComparisonExpression();

            while (this.TryAdvance(TokenKind.XorKeyword)) {
                var second = this.ComparisonExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntaxA(loc, first, second, BinaryOperation.Xor);
            }

            return first;
        }

        private ISyntaxA ComparisonExpression() {
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
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntaxA(loc, first, second, op);
            }

            return first;
        }

        private ISyntaxA AndExpression() {
            var first = this.AddExpression();

            while (this.TryAdvance(TokenKind.AndKeyword)) {
                var second = this.AddExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntaxA(loc, first, second, BinaryOperation.And);
            }

            return first;
        }

        private ISyntaxA AddExpression() {
            var first = this.MultiplyExpression();

            while (true) {
                if (!this.Peek(TokenKind.AddSign) && !this.Peek(TokenKind.SubtractSign)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.AddSign ? BinaryOperation.Add : BinaryOperation.Subtract;
                var second = this.MultiplyExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntaxA(loc, first, second, op);
            }

            return first;
        }

        private ISyntaxA MultiplyExpression() {
            var first = this.SuffixExpression();

            while (true) {
                if (!this.Peek(TokenKind.MultiplySign) && !this.Peek(TokenKind.ModuloSign)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = tok == TokenKind.MultiplySign ? BinaryOperation.Multiply : BinaryOperation.Modulo;
                var second = this.SuffixExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntaxA(loc, first, second, op);
            }

            return first;
        }

        private ISyntaxA SuffixExpression() {
            var first = this.Atom();

            while (this.Peek(TokenKind.OpenParenthesis) || this.Peek(TokenKind.OpenBracket) || this.Peek(TokenKind.LiteralSign) || this.Peek(TokenKind.Dot)) {
                if (this.Peek(TokenKind.OpenParenthesis)) {
                    first = this.InvokeExpression(first);
                }
                else if (this.Peek(TokenKind.OpenBracket)) {
                    first = this.ArrayIndexExpression(first);
                }
                else if (this.Peek(TokenKind.Dot)) {
                    first = this.MemberAccess(first);
                }
                else {
                    first = this.LiteralArrayIndexExpression(first);
                }
            }

            return first;
        }

        private ISyntaxA MemberAccess(ISyntaxA first) {
            this.Advance(TokenKind.Dot);
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);

            if (this.TryAdvance(TokenKind.OpenParenthesis)) {
                var args = new List<ISyntaxA>();

                while (!this.Peek(TokenKind.CloseParenthesis)) {
                    args.Add(this.TopExpression());

                    if (!this.TryAdvance(TokenKind.Comma)) {
                        break;
                    }
                }

                var last = this.Advance(TokenKind.CloseParenthesis);
                var loc = first.Location.Span(last.Location);

                return new MemberInvokeSyntaxA(loc, first, tok.Value, args);
            }
            else {
                var loc = first.Location.Span(tok.Location);

                return new MemberAccessSyntaxA(loc, first, tok.Value);
            }
        }

        private ISyntaxA InvokeExpression(ISyntaxA first) {
            this.Advance(TokenKind.OpenParenthesis);

            var args = new List<ISyntaxA>();

            while (!this.Peek(TokenKind.CloseParenthesis)) {
                args.Add(this.TopExpression());

                if (!this.TryAdvance(TokenKind.Comma)) {
                    break;
                }
            }

            var last = this.Advance(TokenKind.CloseParenthesis);
            var loc = first.Location.Span(last.Location);

            return new FunctionInvokeSyntaxA(loc, first, args);
        }

        private ISyntaxA ArrayIndexExpression(ISyntaxA first) {
            this.Advance(TokenKind.OpenBracket);
            var index = this.TopExpression();
            var last = this.Advance(TokenKind.CloseBracket);
            var loc = first.Location.Span(last.Location);

            return new ArrayAccessSyntaxA(loc, first, index, ArrayAccessKind.ValueAccess);
        }

        private ISyntaxA LiteralArrayIndexExpression(ISyntaxA first) {
            this.Advance(TokenKind.LiteralSign);
            this.Advance(TokenKind.OpenBracket);

            if (this.TryAdvance(TokenKind.Colon)) {
                var index2 = this.TopExpression();
                var last = this.Advance(TokenKind.CloseBracket);
                var loc = first.Location.Span(last.Location);

                return new ArraySliceSyntaxA(loc, first, Option.None<ISyntaxA>(), Option.Some(index2));
            }
            else {
                var index = this.TopExpression();

                if (this.TryAdvance(TokenKind.Colon)) {
                    if (this.Peek(TokenKind.CloseBracket)) {
                        var last = this.Advance(TokenKind.CloseBracket);
                        var loc = first.Location.Span(last.Location);

                        return new ArraySliceSyntaxA(loc, first, Option.Some(index), Option.None<ISyntaxA>());
                    }
                    else {
                        var index2 = this.TopExpression();
                        var last = this.Advance(TokenKind.CloseBracket);
                        var loc = first.Location.Span(last.Location);

                        return new ArraySliceSyntaxA(loc, first, Option.Some(index), Option.Some(index2));
                    }
                }
                else {
                    var last = this.Advance(TokenKind.CloseBracket);
                    var loc = first.Location.Span(last.Location);

                    return new ArrayAccessSyntaxA(loc, first, index, ArrayAccessKind.LiteralAccess);
                }
            }
        }

        private ISyntaxA Atom() {
            if (this.Peek(TokenKind.Identifier)) {
                return this.VariableAccess();
            }
            else if (this.Peek(TokenKind.LiteralSign)) {
                return this.LiteralVariableAccess();
            }
            else if (this.Peek(TokenKind.IntLiteral)) {
                return this.IntLiteral();
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.Block();
            }
            else if (this.Peek(TokenKind.VoidKeyword)) {
                return this.VoidLiteral();
            }
            else if (this.Peek(TokenKind.OpenBracket)) {
                return this.ArrayLiteral();
            }
            else if (this.Peek(TokenKind.Pipe)) {
                return this.ReadOnlyArrayLiteral();
            }
            else if (this.Peek(TokenKind.OpenParenthesis)) {
                return this.ParenExpression();
            }
            else if (this.Peek(TokenKind.BoolLiteral)) {
                return this.BoolLiteral();
            }
            else if (this.Peek(TokenKind.IfKeyword)) {
                return this.IfExpression();
            }
            else if (this.Peek(TokenKind.FromKeyword)) {
                return this.FromExpression();
            }
            else if (this.Peek(TokenKind.RegionKeyword)) {
                return this.RegionExpression();
            }
            else if (this.Peek(TokenKind.NewKeyword)) {
                return this.NewExpression();
            }
            else if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.ClosureExpression();
            }
            else {
                var next = this.Advance();

                throw ParsingErrors.UnexpectedToken(next);
            }
        }

        private ISyntaxA LiteralVariableAccess() {
            var start = this.Advance(TokenKind.LiteralSign);
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);
            var loc = start.Location.Span(tok.Location);

            return new IdentifierAccessSyntaxA(loc, tok.Value, VariableAccessKind.LiteralAccess);
        }

        private ISyntaxA VariableAccess() {
            var tok = (Token<string>)this.Advance(TokenKind.Identifier);

            return new IdentifierAccessSyntaxA(tok.Location, tok.Value, VariableAccessKind.ValueAccess);
        }

        private ISyntaxA IntLiteral() {
            var tok = (Token<int>)this.Advance(TokenKind.IntLiteral);

            return new IntLiteralSyntax(tok.Location, tok.Value);
        }

        private ISyntaxA BoolLiteral() {
            var start = (Token<bool>)this.Advance(TokenKind.BoolLiteral);

            return new BoolLiteralSyntax(start.Location, start.Value);
        }

        private ISyntaxA VoidLiteral() {
            var tok = this.Advance(TokenKind.VoidKeyword);

            return new VoidLiteralAB(tok.Location);
        }

        private ISyntaxA ArrayLiteral() {
            var start = this.Advance(TokenKind.OpenBracket);
            var args = new List<ISyntaxA>();

            while (!this.Peek(TokenKind.CloseBracket)) {
                args.Add(this.TopExpression());

                if (!this.TryAdvance(TokenKind.Comma)) {
                    break;
                }
            }

            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Location.Span(end.Location);

            return new ArrayLiteralSyntaxA(loc, false, args);
        }

        private ISyntaxA ReadOnlyArrayLiteral() {
            var start = this.Advance(TokenKind.Pipe);
            var args = new List<ISyntaxA>();

            while (!this.Peek(TokenKind.Pipe)) {
                args.Add(this.TopExpression());

                if (!this.TryAdvance(TokenKind.Comma)) {
                    break;
                }
            }

            var end = this.Advance(TokenKind.Pipe);
            var loc = start.Location.Span(end.Location);

            return new ArrayLiteralSyntaxA(loc, true, args);
        }

        private ISyntaxA ParenExpression() {
            this.Advance(TokenKind.OpenParenthesis);
            var result = this.TopExpression();
            this.Advance(TokenKind.CloseParenthesis);

            return result;
        }

        private ISyntaxA Block() {
            var start = this.Advance(TokenKind.OpenBrace);
            var stats = new List<ISyntaxA>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                stats.Add(this.Statement());
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            return new BlockSyntaxA(loc, stats);
        }

        private ISyntaxA Statement() {
            ISyntaxA result;

            if (this.Peek(TokenKind.WhileKeyword)) {
                result = this.WhileStatement();
            }
            else if (this.Peek(TokenKind.ForKeyword)) {
                result = this.ForStatement();
            }
            else if (this.Peek(TokenKind.VarKeyword) || this.Peek(TokenKind.RefKeyword)) {
                result = this.VarRefStatement();
            }
            else {
                result = this.StoreStatement();
            }

            this.Advance(TokenKind.Semicolon);

            return result;
        }

        private ISyntaxA WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.DoKeyword);
            var body = this.TopExpression();
            var loc = start.Location.Span(body.Location);

            return new WhileSyntaxA(loc, cond, body);
        }

        private ISyntaxA ForStatement() {
            var start = this.Advance(TokenKind.ForKeyword);
            var id = this.Advance<string>();

            this.Advance(TokenKind.AssignmentSign);
            var startIndex = this.TopExpression();

            this.Advance(TokenKind.ToKeyword);
            var endIndex = this.TopExpression();

            this.Advance(TokenKind.DoKeyword);
            var body = this.TopExpression();
            var loc = start.Location.Span(body.Location);

            return new ForSyntaxA(loc, id, startIndex, endIndex, body);
        }

        private ISyntaxA VarRefStatement() {
            IToken tok;
            bool isReadOnly;

            if (this.Peek(TokenKind.VarKeyword)) {
                tok = this.Advance(TokenKind.VarKeyword);
                isReadOnly = false;
            }
            else {
                tok = this.Advance(TokenKind.RefKeyword);
                isReadOnly = true;
            }

            var name = this.Advance<string>();
            this.Advance(TokenKind.LeftArrow);

            var assign = this.TopExpression();
            var loc = tok.Location.Span(assign.Location);

            return new VarRefSyntaxA(loc, name, assign, isReadOnly);
        }

        private ISyntaxA StoreStatement() {
            var start = this.TopExpression();

            if (this.TryAdvance(TokenKind.LeftArrow)) {
                var assign = this.TopExpression();
                var loc = start.Location.Span(assign.Location);

                return new StoreSyntaxA(loc, start, assign);
            }

            return start;
        }

        private ISyntaxA IfExpression() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();

            if (this.TryAdvance(TokenKind.ThenKeyword)) {
                var affirm = this.TopExpression();

                this.Advance(TokenKind.ElseKeyword);
                var neg = this.TopExpression();
                var loc = start.Location.Span(neg.Location);

                return new IfSyntaxA(loc, cond, affirm, neg);
            }
            else {
                this.Advance(TokenKind.DoKeyword);
                var affirm = this.TopExpression();
                var loc = start.Location.Span(affirm.Location);

                return new IfSyntaxA(loc, cond, affirm);
            }
        }

        private ISyntaxA FromExpression() {
            var start = this.Advance(TokenKind.FromKeyword);
            var region = this.Lifetime();

            this.Advance(TokenKind.DoKeyword);
            var expr = this.TopExpression();
            var loc = start.Location.Span(expr.Location);

            return new FromSyntaxA(loc, region, expr);
        }

        private ISyntaxA RegionExpression() {
            var start = this.Advance(TokenKind.RegionKeyword);

            if (this.Peek(TokenKind.OpenBrace)) {
                var body = this.Block();
                var loc = start.Location.Span(body.Location);

                return new RegionBlockSyntaxA(loc, body);
            }
            else {
                var name = this.Advance<string>();
                var body = this.Block();
                var loc = start.Location.Span(body.Location);

                return new RegionBlockSyntaxA(loc, body, name);
            }
        }

        private ISyntaxA NewExpression() {
            var start = this.Advance(TokenKind.NewKeyword);
            var targetType = this.TypeExpression();
            var mems = new List<StructArgument<ISyntaxA>>();

            if (!this.TryAdvance(TokenKind.OpenBrace)) {
                return new NewSyntaxA(start.Location, targetType, mems);
            }

            while (!this.Peek(TokenKind.CloseBrace)) {
                var memName = this.Advance<string>();
                this.Advance(TokenKind.AssignmentSign);

                var memValue = this.TopExpression();

                mems.Add(new StructArgument<ISyntaxA>() {
                    MemberName = memName,
                    MemberValue = memValue
                });

                if (!this.Peek(TokenKind.CloseBrace)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            return new NewSyntaxA(loc, targetType, mems);
        }

        private ISyntaxA ClosureExpression() {
            var start = this.Advance(TokenKind.FunctionKeyword);

            this.Advance(TokenKind.OpenParenthesis);

            var pars = ImmutableList<FunctionParameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var parName = this.Advance<string>();
                this.Advance(TokenKind.AsKeyword);
                var parType = this.TypeExpression();

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new FunctionParameter(parName, parType));
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.YieldSign);

            var body = this.TopExpression();
            var loc = start.Location.Span(body.Location);

            return new LambdaSyntaxA(loc, body, pars);
        }
    }
}