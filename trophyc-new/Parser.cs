using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing {
    public enum NameTarget {
        Struct, Union, Function, Variable
    }

    public class NameTable {
        public Dictionary<string, FunctionSignature> FunctionSignatures { get; } = new Dictionary<string, FunctionSignature>();

        public Dictionary<string, CompositeSignature> StructSignatures { get; } = new Dictionary<string, CompositeSignature>();

        public Dictionary<string, CompositeSignature> UnionSignatures { get; } = new Dictionary<string, CompositeSignature>();

        public HashSet<string> Variables { get; } = new HashSet<string>();

        public bool TryGetPath(string path, out NameTarget target) {
            if (this.FunctionSignatures.TryGetValue(path, out _)) {
                target = NameTarget.Function;
                return true;
            }
            else if (this.StructSignatures.TryGetValue(path, out _)) {
                target = NameTarget.Struct;
                return true;
            }
            else if (this.UnionSignatures.TryGetValue(path, out _)) {
                target = NameTarget.Union;
                return true;
            }
            else if (this.Variables.Contains(path)) {
                target = NameTarget.Variable;
                return true;
            }
            else {
                target = default;
                return false;
            }
        }

        public bool DoesPathShadow(string path) {
            var segments = path.Split('$');

            if (segments.Length <= 1) {
                return false;
            }

            var name = segments.Last();
            var scope = string.Join('$', segments.Take(segments.Length - 2));

            return this.TryFindPath(scope, name, out _, out _);
        }

        public bool TryFindPath(string scope, string name, out string path, out NameTarget target) {
            var segments = scope.Split('$');

            for (int i = segments.Length; i >= 0; i--) {
                path = string.Join('$', segments.Take(i).Append(name));

                if (this.TryGetPath(path, out target)) {
                    return true;
                }
            }

            target = default;
            path = default;

            return false;
        }
    }

    public class Parser {
        private readonly Lexer lex;
        private readonly Stack<string> path;

        public event EventHandler<ParseException> ErrorDiscovered;

        public NameTable NameTable { get; } = new NameTable();

        public Parser(string[] lines) {
            this.lex = new Lexer(lines);
        }

        /** Helper Methods **/
        private string GetPath(string name) {
            return string.Join('$', this.path.Append(name));
        }

        private bool RequireToken(TokenKind next) {
            return this.RequireToken(next, out _);
        }

        private bool RequireToken(TokenKind next, out Token tok) {
            if (this.lex.NextIf(next, out tok)) {
                return true;
            }
            else {
                this.ErrorDiscovered?.Invoke(this, ParseException.UnexpectedToken(next, this.lex.Peek()));
                return false;
            }
        }

        private bool RequireString(out string result) {
            if (this.RequireToken(TokenKind.Identifier, out var tok)) {
                result = tok.Payload.ToString();
                return true;
            }
            else {
                result = null;
                return false;
            }
        }

        private void ConsumeUntil(TokenKind kind) {
            while (true) {
                if (kind != TokenKind.Semicolon && this.lex.Peek(TokenKind.Semicolon)) {
                    break;
                }
                else if (this.lex.Peek(kind)) {
                    this.lex.Next();
                    break;
                }
                else {
                    this.lex.Next();
                }
            }
        }

        /** Declaration Parsing **/
        public IParseDeclaration ParseDeclaration() {
            if (this.lex.Peek(TokenKind.StructKeyword) || this.lex.Peek(TokenKind.UnionKeyword)) {
                return this.ParseCompositeDeclaration();
            }
            else if (this.lex.Peek(TokenKind.FunctionKeyword)) {
                return this.ParseFunctionDeclaration();
            }
            else {
                this.ErrorDiscovered?.Invoke(this, ParseException.UnexpectedToken(this.lex.Peek()));
                return new ErrorDeclaration();
            }
        }

        private bool TryParseFunctionSignature(out FunctionSignature sig, out Token first) {
            var name = "";

            // Parse leading tokens
            var success = this.RequireToken(TokenKind.FunctionKeyword, out first)
                && this.RequireString(out name)
                && this.RequireToken(TokenKind.OpenParenthesis);

            if (!success) {
                sig = null;
                return false;
            }

            // Parse parameters
            var pars = new List<Parameter>();
            while (!this.lex.NextIf(TokenKind.CloseParenthesis)) {
                var parName = "";
                var parType = ITrophyType.Void;

                success = this.RequireString(out parName)
                    && this.RequireToken(TokenKind.AsKeyword)
                    && this.TryParseTypeExpression(out parType);

                if (!this.lex.Peek(TokenKind.CloseParenthesis)) {
                    success &= this.RequireToken(TokenKind.Comma);
                }

                if (!success) {
                    this.ConsumeUntil(TokenKind.CloseParenthesis);

                    sig = null;
                    return false;
                }

                pars.Add(new Parameter(parName, parType));
            }

            var returnType = ITrophyType.Void;

            success = this.RequireToken(TokenKind.AsKeyword)
                && this.TryParseTypeExpression(out returnType);

            if (!success) {
                sig = null;
                return false;
            }

            sig = new FunctionSignature(name, returnType, pars.ToImmutableList());
            return true;
        }

        public IParseDeclaration ParseFunctionDeclaration() {
            if (!this.TryParseFunctionSignature(out var sig, out var first)) {
                return new ErrorDeclaration();
            }

            var path = this.GetPath(sig.Name);

            // Put this function in the type table and the name table
            this.NameTable.FunctionSignatures.Add(path, sig);

            if (!this.RequireToken(TokenKind.DoubleRightArrow)) {
                return new ErrorDeclaration();
            }

            // Parse the body
            this.path.Push(sig.Name);
            var body = this.ParseExpression();
            this.path.Pop();

            // Keep going even if this wasn't found
            this.RequireToken(TokenKind.Semicolon);

            var loc = first.Location.Span(body.Location);

            return new FunctionDeclaration(loc, path, sig, body);
        }

        public IParseDeclaration ParseCompositeDeclaration() {
            // Parse the leading keyword
            if (!this.lex.NextIf(TokenKind.StructKeyword, out Token first)) {
                if (!this.RequireToken(TokenKind.UnionKeyword, out first)) {
                    return new ErrorDeclaration();
                }
            }

            // Parse the name and open brace
            if (!this.RequireString(out string name) || !this.RequireToken(TokenKind.OpenBrace)) {
                return new ErrorDeclaration();
            }

            // Put this struct/union in the name table
            var path = this.GetPath(name);

            // Parse the members
            var members = new List<Parameter>();
            while (this.lex.Peek(TokenKind.Identifier)) {
                ITrophyType memType = null;

                var success = this.RequireString(out string memName)
                    && this.RequireToken(TokenKind.AsKeyword)
                    && this.TryParseTypeExpression(out memType)
                    && this.RequireToken(TokenKind.Semicolon);

                if (!success) {
                    this.ConsumeUntil(TokenKind.CloseBrace);
                    return new ErrorDeclaration();
                }

                members.Add(new Parameter(memName, memType));
            }

            // Parse the inner declarations
            var inner_decls = new List<IParseDeclaration>();
            this.path.Push(name);

            while (!this.lex.Peek(TokenKind.CloseBrace)) {
                inner_decls.Add(this.ParseDeclaration());
            }

            this.path.Pop();

            // Parse the closing brace and semicolon
            if (!this.RequireToken(TokenKind.CloseBrace)) {
                return new ErrorDeclaration();
            }

            // Keep going even if there's not a semicolon
            this.RequireToken(TokenKind.Semicolon, out var last);

            var kind = first.Kind == TokenKind.StructKeyword ? CompositeKind.Struct : CompositeKind.Union;
            var sig = new CompositeSignature(name, members);
            var loc = first.Location.Span(last.Location);

            // Put this struct/union in the typetable
            if (first.Kind == TokenKind.StructKeyword) {
                this.NameTable.StructSignatures.Add(path, sig);
            }
            else {
                this.NameTable.UnionSignatures.Add(path, sig);
            }

            return new CompositeDeclaration(loc, path, kind, sig, inner_decls);
        }

        /** Expression Parsing **/
        public IParseExpression ParseExpression() {
            if (this.lex.Peek(TokenKind.OpenBrace)) {
                return this.BlockExpression();
            }
            else {
                return this.AtomExpression();
            }
        }

        public IParseExpression BlockExpression() {
            if (!this.RequireToken(TokenKind.OpenBrace, out var first)) {
                return new ErrorExpression();
            }

            var stats = new List<IParseExpression>();

            while (!this.lex.Peek(TokenKind.CloseBrace)) {
                stats.Add(this.ParseStatement());
            }

            if (!this.RequireToken(TokenKind.CloseBrace, out var last)) {
                return new ErrorExpression();
            }

            var loc = first.Location.Span(last.Location);

            return new BlockExpression(loc, stats);
        }

        public IParseExpression AtomExpression() {
            if (this.lex.NextIf(TokenKind.IntLiteral, out var tok)) {
                int value = (int)tok.Payload;

                return new IntLiteral(tok.Location, value);
            }
            else if (this.lex.NextIf(TokenKind.OpenParenthesis)) {
                var result = this.ParseExpression();

                if (!this.RequireToken(TokenKind.CloseParenthesis)) {
                    return new ErrorExpression();
                }

                return result;
            }
            else if (this.lex.NextIf(TokenKind.Identifier, out tok)) {
                var name = tok.Payload.ToString();

                return new VariableAccess(tok.Location, name);
            }
            else {
                this.ErrorDiscovered?.Invoke(this, ParseException.UnexpectedToken(this.lex.Peek()));
                return new ErrorExpression();
            }
        }

        /** Statement Parsing **/
        public IParseExpression ParseStatement() {
            IParseExpression result;

            if (this.lex.Peek(TokenKind.LocKeyword)) {
                result = this.ParseVariableStatement();
            }
            else {
                result = this.ParseExpression();
            }

            this.ConsumeUntil(TokenKind.Semicolon);

            return result;
        }

        public IParseExpression ParseVariableStatement() {
            string name = null;

            var success = this.RequireToken(TokenKind.LocKeyword, out var first)
                && this.RequireString(out name)
                && this.RequireToken(TokenKind.SingleEqualsSign);

            if (!success) {
                return new ErrorExpression();
            }

            var assign = this.ParseExpression();
            var loc = first.Location.Span(assign.Location);
            var path = this.GetPath(name);

            return new VariableStatement(loc, name, path, assign);
        }

        /** Type Parsing **/
        private bool TryParseTypeExpression(out ITrophyType type) {
            if (this.lex.Peek(TokenKind.VarKeyword) || this.lex.Peek(TokenKind.RefKeyword)) {
                return this.TryParseVariableType(out type);
            }

            return this.TryParseTypeAtom(out type);
        }

        private bool TryParseVariableType(out ITrophyType type) {
            Token first;

            if (!this.lex.NextIf(TokenKind.VarKeyword, out first)) {
                if (!this.RequireToken(TokenKind.RefKeyword, out first)) {
                    type = null;
                    return false;
                }
            }

            this.RequireToken(TokenKind.OpenBracket);

            if (!this.TryParseTypeExpression(out var innerType)) {
                this.ConsumeUntil(TokenKind.CloseBracket);

                type = null;
                return false;
            }

            this.RequireToken(TokenKind.CloseBracket);

            var isReadOnly = first.Kind == TokenKind.RefKeyword;

            type = new VariableType(innerType, isReadOnly);
            return true;
        }

        private bool TryParseTypeAtom(out ITrophyType type) {
            if (this.lex.NextIf(TokenKind.IntKeyword)) {
                type = ITrophyType.Integer;
                return true;
            }
            if (this.lex.NextIf(TokenKind.BoolKeyword)) {
                type = ITrophyType.Boolean;
                return true;
            }
            if (this.lex.NextIf(TokenKind.VoidKeyword)) {
                type = ITrophyType.Void;
                return true;
            }
            else if (this.lex.Peek(TokenKind.ArrayKeyword) || this.lex.Peek(TokenKind.SpanKeyword)) {
                return this.TryParseArrayTypeAtom(out type);
            }
            else if (this.lex.Peek(TokenKind.FunctionKeyword)) {
                return this.TryParseFunctionTypeAtom(out type);
            }
            else {
                type = null;
                return false;
            }
        }

        private bool TryParseArrayTypeAtom(out ITrophyType result) {
            if (!this.lex.NextIf(TokenKind.SpanKeyword, out var start)) {
                if (!this.RequireToken(TokenKind.ArrayKeyword, out start)) {
                    result = null;
                    return false;
                }
            }

            this.RequireToken(TokenKind.OpenBracket);

            if (!this.TryParseTypeExpression(out var innerType)) {
                this.ConsumeUntil(TokenKind.CloseBracket);

                result = null;
                return false;
            }

            this.RequireToken(TokenKind.CloseBracket);

            var isReadOnly = start.Kind == TokenKind.SpanKeyword;
            result = new ArrayType(innerType, isReadOnly);

            return true;
        }

        private bool TryParseFunctionTypeAtom(out ITrophyType type) {
            this.RequireToken(TokenKind.FunctionKeyword);
            this.RequireToken(TokenKind.OpenBracket);

            if (!this.TryParseTypeExpression(out var returnType)) {
                this.ConsumeUntil(TokenKind.CloseBracket);

                type = null;
                return false;
            }

            var args = new List<ITrophyType>();

            while (this.lex.NextIf(TokenKind.Comma)) {
                if (!this.TryParseTypeExpression(out var argType)) {
                    this.ConsumeUntil(TokenKind.CloseBracket);

                    type = null;
                    return false;
                }

                args.Add(argType);
            }

            this.RequireToken(TokenKind.CloseBracket);

            type = new FunctionType(returnType, args);
            return true;
        }
    }
}