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
using Trophy.Features.Meta;
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
        private ISyntaxA VarTypeExpression() {
            if (this.Peek(TokenKind.VarKeyword)) {
                var start = this.Advance(TokenKind.VarKeyword);

                this.Advance(TokenKind.OpenBracket);
                var inner = this.TypeExpression();
                var end = this.Advance(TokenKind.CloseBracket);
                var loc = start.Location.Span(end.Location);

                return new VarRefTypeSyntaxA(loc, inner, false);
            }
            else {
                var start = this.Advance(TokenKind.RefKeyword);

                this.Advance(TokenKind.OpenBracket);
                var inner = this.TypeExpression();
                var end = this.Advance(TokenKind.CloseBracket);
                var loc = start.Location.Span(end.Location);

                return new VarRefTypeSyntaxA(loc, inner, true);
            }
        }

        private ISyntaxA TypeExpression() {
            if (this.Peek(TokenKind.VarKeyword) || this.Peek(TokenKind.RefKeyword)) {
                return this.VarTypeExpression();
            }

            return this.TypeAccess();
        }

        private ISyntaxA TypeAccess() {
            var first = TypeAtom();

            while (this.TryAdvance(TokenKind.Dot)) {
                var tok = (Token<string>)this.Advance(TokenKind.Identifier);
                var loc = first.Location.Span(tok.Location);

                first = new MemberAccessSyntaxA(loc, first, tok.Value, false);
            }

            return first;
        }

        private ISyntaxA TypeAtom() {
            if (this.Peek(TokenKind.IntKeyword)) {
                var tok = this.Advance(TokenKind.IntKeyword);

                return new TypeAccessSyntaxA(tok.Location, ITrophyType.Integer);
            }
            else if (this.Peek(TokenKind.VoidKeyword)) {
                var tok = this.Advance(TokenKind.VoidKeyword);

                return new TypeAccessSyntaxA(tok.Location, ITrophyType.Void);
            }
            else if (this.Peek(TokenKind.BoolKeyword)) {
                var tok = this.Advance(TokenKind.BoolKeyword);

                return new TypeAccessSyntaxA(tok.Location, ITrophyType.Boolean);
            }
            else if (this.Peek(TokenKind.ArrayKeyword) || this.Peek(TokenKind.SpanKeyword)) {
                return this.ArrayTypeAtom();
            }
            else if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.FunctionTypeAtom();
            }
            else {
                Token<string> tok = (Token<string>)this.Advance(TokenKind.Identifier);

                return new TypeAccessSyntaxA(tok.Location, new NamedType(new IdentifierPath(tok.Value)));
            }
        }

        private ISyntaxA ArrayTypeAtom() {
            TokenLocation start;
            bool isReadOnly;

            if (this.Peek(TokenKind.SpanKeyword)) {
                start = this.Advance(TokenKind.SpanKeyword).Location;
                isReadOnly = true;
            }
            else {
                start = this.Advance(TokenKind.ArrayKeyword).Location;
                isReadOnly = false;
            }

            this.Advance(TokenKind.OpenBracket);

            var inner = this.TypeExpression();
            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Span(end.Location);

            return new ArrayTypeSyntaxA(loc, inner, isReadOnly);
        }

        private ISyntaxA FunctionTypeAtom() {
            var start = this.Advance(TokenKind.FunctionKeyword);
            this.Advance(TokenKind.OpenBracket);

            var args = new List<ISyntaxA>();

            while (!this.TryAdvance(TokenKind.YieldSign)) {
                args.Add(this.TypeExpression());

                if (!this.TryAdvance(TokenKind.Comma)) {
                    continue;
                }
            }

            var returnType = this.TypeExpression();
            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Location.Span(end.Location);

            return new FunctionTypeSyntaxA(loc, returnType, args);
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
        private ParseFunctionSignature FunctionSignature() {
            this.Advance(TokenKind.FunctionKeyword);
            var funcName = this.Advance<string>();

            this.Advance(TokenKind.OpenParenthesis);

            var pars = ImmutableList<ParseFunctionParameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var kind = VariableKind.Value;

                if (this.TryAdvance(TokenKind.VarKeyword)) {
                    kind = VariableKind.VarVariable;
                }
                else if (this.TryAdvance(TokenKind.RefKeyword)) {
                    kind = VariableKind.RefVariable;
                }

                var parName = this.Advance<string>();
                this.Advance(TokenKind.AsKeyword);
                var parType = this.TypeExpression();

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new ParseFunctionParameter(parName, parType, kind));
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.AsKeyword);            
            
            var returnType = this.TypeExpression();
            var sig = new ParseFunctionSignature(funcName, returnType, pars);

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
            var mems = new List<ParseAggregateMember>();
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
                var varkind = VariableKind.Value;

                if (this.TryAdvance(TokenKind.VarKeyword)) {
                    varkind = VariableKind.VarVariable;
                }
                else if (this.TryAdvance(TokenKind.RefKeyword)) {
                    varkind = VariableKind.RefVariable;
                }

                var memName = this.Advance<string>();
                this.Advance(TokenKind.AsKeyword);
                var memType = this.TypeExpression();

                this.Advance(TokenKind.Semicolon);
                mems.Add(new ParseAggregateMember(memName, memType, varkind));
            }

            while (!this.Peek(TokenKind.CloseBrace)) {
                decls.Add(this.Declaration());
            }

            this.Advance(TokenKind.CloseBrace);
            var last = this.Advance(TokenKind.Semicolon);
            var loc = start.Location.Span(last.Location);
            var sig = new ParseAggregateSignature(name, mems);
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
        private ISyntaxA TopExpression() => this.AsIsExpression();

        private ISyntaxA AsIsExpression() {
            var first = this.OrExpression();

            while (this.Peek(TokenKind.AsKeyword) || this.Peek(TokenKind.IsKeyword)) {
                if (this.TryAdvance(TokenKind.AsKeyword)) {
                    var target = this.TypeExpression();
                    var loc = first.Location.Span(this.tokens[this.pos - 1].Location);

                    first = new AsSyntaxA(loc, first, target);
                }
                else {
                    this.Advance(TokenKind.IsKeyword);
                    var negate = this.TryAdvance(TokenKind.NotKeyword);

                    var pattern = this.Advance<string>();
                    var patternName = Option.None<string>();

                    if (this.Peek(TokenKind.Identifier)) {
                        patternName = Option.Some(this.Advance<string>());
                    }

                    var loc = first.Location.Span(this.tokens[this.pos - 1].Location);

                    first = new IsSyntaxA(loc, first, pattern, patternName);

                    if (negate) {
                        first = new UnarySyntaxA(loc, UnaryOperator.Not, first);
                    }
                }
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
            var first = this.PrefixExpression();

            while (true) {
                if (!this.Peek(TokenKind.MultiplySign) && !this.Peek(TokenKind.ModuloSign) && !this.Peek(TokenKind.SlashSign)) {
                    break;
                }

                var tok = this.Advance().Kind;
                var op = BinaryOperation.Modulo;

                if (tok == TokenKind.MultiplySign) {
                    op = BinaryOperation.Multiply;
                }
                else if (tok == TokenKind.SlashSign) {
                    op = BinaryOperation.FloorDivide;
                }

                var second = this.PrefixExpression();
                var loc = first.Location.Span(second.Location);

                first = new BinarySyntaxA(loc, first, second, op);
            }

            return first;
        }

        private ISyntaxA PrefixExpression() {
            if (this.Peek(TokenKind.SubtractSign) || this.Peek(TokenKind.AddSign) || this.Peek(TokenKind.NotSign)) {
                var tokOp = this.Advance();
                var first = this.SuffixExpression();
                var loc = tokOp.Location.Span(first.Location);
                var op = UnaryOperator.Not;

                if (tokOp.Kind == TokenKind.AddSign) {
                    op = UnaryOperator.Plus;
                }
                else if (tokOp.Kind == TokenKind.SubtractSign) {
                    op = UnaryOperator.Minus;
                }

                return new UnarySyntaxA(loc, op, first);
            }

            return this.SuffixExpression();
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
            
            if (this.TryAdvance(TokenKind.LiteralSign)) {
                var tok1 = (Token<string>)this.Advance(TokenKind.Identifier);

                return new MemberAccessSyntaxA(tok1.Location, first, tok1.Value, true);
            }

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

                return new MemberAccessSyntaxA(loc, first, tok.Value, false);
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
            else if (this.Peek(TokenKind.NewKeyword) || this.Peek(TokenKind.PutKeyword)) {
                return this.NewPutExpression();
            }
            else if (this.Peek(TokenKind.MatchKeyword)) {
                return this.MatchExpression();
            }
            else if (this.Peek(TokenKind.VarKeyword) || this.Peek(TokenKind.RefKeyword)) {
                return this.VarRefExpression();
            }
            else {
                var next = this.Advance();

                throw ParsingErrors.UnexpectedToken(next);
            }
        }

        private ISyntaxA VarRefExpression() {
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
            this.Advance(TokenKind.AssignmentSign);

            var assign = this.TopExpression();
            var loc = tok.Location.Span(assign.Location);

            return new VarRefSyntaxA(loc, name, assign, isReadOnly);
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

        private ISyntaxA ArrayLiteral(bool isStackAllocated) {
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

            return new ArrayLiteralSyntaxA(loc, false, args, isStackAllocated);
        }

        private ISyntaxA ReadOnlyArrayLiteral(bool isStackAllocated) {
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

            return new ArrayLiteralSyntaxA(loc, true, args, isStackAllocated);
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
            else if (this.Peek(TokenKind.ReturnKeyword)) {
                result = this.ReturnStatement();
            }
            else if (this.Peek(TokenKind.AsyncKeyword)) {
                result = this.AsyncStatement();
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

        private ISyntaxA StoreStatement() {
            var start = this.TopExpression();

            if (this.TryAdvance(TokenKind.AssignmentSign)) {
                var assign = this.TopExpression();
                var loc = start.Location.Span(assign.Location);

                return new StoreSyntaxA(loc, start, assign);
            }

            return start;
        }

        private ISyntaxA ReturnStatement() {
            var start = this.Advance(TokenKind.ReturnKeyword);
            var arg = this.TopExpression();
            var loc = start.Location.Span(arg.Location);

            return new ReturnSyntaxA(loc, arg);
        }

        private ISyntaxA AsyncStatement() {
            var start = this.Advance(TokenKind.AsyncKeyword);
            var body = this.TopExpression();
            var loc = start.Location.Span(body.Location);

            return new AsyncSyntaxA(loc, body);
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

        private ISyntaxA NewPutExpression() {
            TokenLocation start;
            bool isStackAllocated;

            if (this.Peek(TokenKind.NewKeyword)) {
                start = this.Advance(TokenKind.NewKeyword).Location;
                isStackAllocated = false;
            }
            else {
                start = this.Advance(TokenKind.PutKeyword).Location;
                isStackAllocated = true;
            }

            if (this.Peek(TokenKind.OpenBracket)) {
                return this.ArrayLiteral(isStackAllocated);
            }
            else if (this.Peek(TokenKind.Pipe)) {
                return this.ReadOnlyArrayLiteral(isStackAllocated);
            }
            else if (this.Peek(TokenKind.FunctionKeyword)) {
                return this.ClosureExpression(isStackAllocated);
            }

            var targetType = this.TypeExpression();
            var mems = new List<StructArgument<ISyntaxA>>();

            if (!this.TryAdvance(TokenKind.OpenBrace)) {
                return new CreateTypeSyntaxA(start, targetType, mems, isStackAllocated);
            }

            while (!this.Peek(TokenKind.CloseBrace)) {
                var tok = (Token<string>)this.Advance(TokenKind.Identifier);
                ISyntaxA memValue;
                
                if (this.TryAdvance(TokenKind.AssignmentSign)) {
                   memValue  = this.TopExpression();
                }
                else {
                    memValue = new VoidLiteralAB(tok.Location);
                }

                mems.Add(new StructArgument<ISyntaxA>() {
                    MemberName = tok.Value,
                    MemberValue = memValue
                });

                if (!this.Peek(TokenKind.CloseBrace)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Span(end.Location);

            return new CreateTypeSyntaxA(loc, targetType, mems, isStackAllocated);
        }

        private ISyntaxA ClosureExpression(bool isStackAllocated) {
            var start = this.Advance(TokenKind.FunctionKeyword);

            this.Advance(TokenKind.OpenParenthesis);

            var pars = ImmutableList<ParseFunctionParameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var kind = VariableKind.Value;

                if (this.TryAdvance(TokenKind.VarKeyword)) {
                    kind = VariableKind.VarVariable;
                }
                else if (this.TryAdvance(TokenKind.RefKeyword)) {
                    kind = VariableKind.RefVariable;
                }

                var parName = this.Advance<string>();
                this.Advance(TokenKind.AsKeyword);
                var parType = this.TypeExpression();

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new ParseFunctionParameter(parName, parType, kind));
            }

            this.Advance(TokenKind.CloseParenthesis);
            this.Advance(TokenKind.YieldSign);

            var body = this.TopExpression();
            var loc = start.Location.Span(body.Location);

            return new LambdaSyntaxA(loc, body, pars, isStackAllocated);
        }

        private ISyntaxA MatchExpression() {
            var start = this.Advance(TokenKind.MatchKeyword);
            var arg = this.TopExpression();
            var patterns = new List<MatchPatternA>();

            while (this.TryAdvance(TokenKind.IfKeyword)) {
                var member = this.Advance<string>();
                var name = Option.None<string>();

                if (!this.Peek(TokenKind.ThenKeyword)) {
                    name = Option.Some(this.Advance<string>());
                }

                this.Advance(TokenKind.ThenKeyword);

                var expr = this.TopExpression();
                var pat = new MatchPatternA(member, name, expr);

                patterns.Add(pat);
            }

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var last = this.TopExpression();
                var loc = start.Location.Span(last.Location);

                return new MatchSyntaxA(loc, arg, patterns, Option.Some(last));
            }
            else {
                var loc = start.Location.Span(patterns.Last().Expression.Location);

                return new MatchSyntaxA(loc, arg, patterns, Option.None<ISyntaxA>());
            }
        }
    }
}