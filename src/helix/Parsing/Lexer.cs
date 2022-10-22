using System.Data.Common;
using Helix.Analysis;

namespace Helix.Parsing {
    public class Lexer {
        private static readonly Dictionary<string, TokenKind> keywords = new() {
            { "var", TokenKind.VarKeyword }, { "let", TokenKind.LetKeyword }, 
            { "func", TokenKind.FunctionKeyword }, { "extern", TokenKind.ExternKeyword },
            { "int", TokenKind.IntKeyword }, { "void", TokenKind.VoidKeyword },
            { "bool", TokenKind.BoolKeyword }, { "as", TokenKind.AsKeyword },
            { "is", TokenKind.IsKeyword }, { "if", TokenKind.IfKeyword },
            { "then", TokenKind.ThenKeyword }, { "else", TokenKind.ElseKeyword },
            { "while", TokenKind.WhileKeyword }, { "for", TokenKind.ForKeyword },
            { "do", TokenKind.DoKeyword }, { "to", TokenKind.ToKeyword },
            { "struct", TokenKind.StructKeyword }, { "union", TokenKind.UnionKeyword },
            { "and", TokenKind.AndKeyword }, { "or", TokenKind.OrKeyword },
            { "xor", TokenKind.XorKeyword }, { "put", TokenKind.PutKeyword },
            { "break", TokenKind.BreakKeyword }, { "continue", TokenKind.ContinueKeyword },
            { "return", TokenKind.ReturnKeyword }
        };

        private static readonly Dictionary<char, TokenKind> symbols = new() {
            { '(', TokenKind.OpenParenthesis }, { ')', TokenKind.CloseParenthesis },
            { '{', TokenKind.OpenBrace }, { '}', TokenKind.CloseBrace },
            { '[', TokenKind.OpenBracket }, { ']', TokenKind.CloseBracket },
            { ',', TokenKind.Comma }, { '.', TokenKind.Dot },
            { ';', TokenKind.Semicolon },
            { '^', TokenKind.Caret }
        };

        private readonly string text;

        private int pos = 0;
        private int line = 1;
        private IdentifierPath scope = new();

        private char Current => this.text[this.pos];

        private TokenLocation Location => new(pos, 1, this.line, this.scope);

        public Lexer(string text) {
            this.text = text;
        }

        private Token GetLessThanOrArrowOrLessThanOrEqualTo() {
            if (pos + 1 < this.text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.LessThanOrEqualTo, new TokenLocation(pos - 1, 2, this.line, scope), "<=");
                }
                else {
                    return new Token(TokenKind.LessThan, Location, "<");
                }
            }
            else {
                throw ParsingErrors.EndOfFile(this.Location);
            }
        }

        private Token GetEqualsOrYieldsOrAssignment() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '>') {
                    pos++;

                    return new Token(TokenKind.Yields, new TokenLocation(pos - 1, 2, line, scope), "=>");
                }
                else if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.Equals, new TokenLocation(pos - 1, 2, line, scope), "==");
                }
                else {
                    return new Token(TokenKind.Assignment, Location, "=");
                }
            }
            else {
                throw ParsingErrors.EndOfFile(this.Location);
            }
        }

        private Token GetGreaterThanOrGreaterThanOrEqualTo() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.GreaterThanOrEqualTo, new TokenLocation(pos - 1, 2, line, scope), ">=");
                }
                else {
                    return new Token(TokenKind.GreaterThan, Location, ">");
                }
            }
            else {
                throw ParsingErrors.EndOfFile(this.Location);
            }
        }

        private Token GetNotOrNotEqual() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.NotEquals, new TokenLocation(pos - 1, 2, line, scope), "!=");
                }
                else {
                    return new Token(TokenKind.Not, Location, "!");
                }
            }
            else {
                throw ParsingErrors.EndOfFile(this.Location);
            }
        }

        private Token GetNumber() {
            int start = pos;
            string strNum = "";

            while (pos < this.text.Length && char.IsDigit(Current)) {
                strNum += this.text[pos];
                pos++;
            }

            pos--;

            var loc = new TokenLocation(start, strNum.Length, line, scope);

            if (int.TryParse(strNum, out int num)) {
                return new Token(TokenKind.IntLiteral, loc, strNum);
            }
            else {
                throw ParsingErrors.InvalidNumber(loc, strNum);
            }
        }

        private Token GetIdentifier() {
            int start = pos;
            string id = "";

            while (pos < this.text.Length && (char.IsLetterOrDigit(Current) || Current == '_')) {
                id += this.text[pos];
                pos++;
            }

            pos--;

            var location = new TokenLocation(start, id.Length, line, scope);

            if (keywords.TryGetValue(id, out var kind)) {
                return new Token(kind, location, id);
            }
            else if (id == "true" || id == "false") {
                return new Token(TokenKind.BoolLiteral, location, id);
            }
            else {
                return new Token(TokenKind.Identifier, location, id);
            }
        }

        private Token GetCharLiteral() {
            // Advance past the first '
            int start = pos++;

            // Get the character
            if (pos >= this.text.Length || !char.IsLetterOrDigit(Current)) {
                throw ParsingErrors.UnexpectedCharacter(Location, Current);
            }

            int c = (int)Current;

            // Advance past the second '
            pos++;
            if (pos >= this.text.Length || Current != '\'') {
                throw ParsingErrors.UnexpectedCharacter(Location, Current);
            }

            return new Token(TokenKind.IntLiteral, new TokenLocation(start, 3, line, scope), c.ToString());
        }

        private Token GetSlashOrCommentOrDivideAssignment() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '/') {
                    int start = pos;

                    while (pos < text.Length && text[pos] != '\n') {
                        pos++;
                    }

                    pos--;

                    var location = new TokenLocation(start, pos - start + 1, line, scope);
                    return new Token(TokenKind.Whitespace, location, "");
                }
                else if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(
                        TokenKind.DivideAssignment, 
                        new TokenLocation(pos - 1, 2, line, scope), "/=");
                }
            }

            return new Token(TokenKind.Divide, Location, "/");            
        }

        private Token GetPlusOrPlusAssignment() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(
                        TokenKind.PlusAssignment,
                        new TokenLocation(pos - 1, 2, line, scope), "+=");
                }
            }

            return new Token(TokenKind.Plus, Location, "+");
        }

        private Token GetMinusOrMinusAssignment() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(
                        TokenKind.MinusAssignment,
                        new TokenLocation(pos - 1, 2, line, scope), "-=");
                }
            }

            return new Token(TokenKind.Minus, Location, "-");
        }

        private Token GetStarOrStarAssignment() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(
                        TokenKind.StarAssignment,
                        new TokenLocation(pos - 1, 2, line, scope), "*=");
                }
            }

            return new Token(TokenKind.Star, Location, "*");
        }

        private Token GetModuloOrModuloAssignment() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(
                        TokenKind.ModuloAssignment,
                        new TokenLocation(pos - 1, 2, line, scope), "%=");
                }
            }

            return new Token(TokenKind.Modulo, Location, "%");
        }

        private Token GetTokenHelper() {
            if (pos >= text.Length) {
                return new Token(TokenKind.EOF, new TokenLocation(), "");
            }

            if (symbols.TryGetValue(Current, out var kind)) {
                return new Token(kind, Location, Current.ToString());
            }

            if (Current == '=') {
                return this.GetEqualsOrYieldsOrAssignment();
            }
            else if (Current == '<') {
                return this.GetLessThanOrArrowOrLessThanOrEqualTo();
            }
            else if (Current == '>') {
                return this.GetGreaterThanOrGreaterThanOrEqualTo();
            }
            else if (Current == '!') {
                return this.GetNotOrNotEqual();
            }
            else if (Current == '\'') {
                return this.GetCharLiteral();
            }
            else if (Current == '+') {
                return this.GetPlusOrPlusAssignment();
            }
            else if (Current == '-') {
                return this.GetMinusOrMinusAssignment();
            }
            else if (Current == '*') {
                return this.GetStarOrStarAssignment();
            }
            else if (Current == '/') {
                return this.GetSlashOrCommentOrDivideAssignment();
            }
            else if (Current == '%') {
                return this.GetModuloOrModuloAssignment();
            }
            else if (char.IsDigit(Current)) {
                return this.GetNumber();
            }
            else if (char.IsLetter(Current)) {
                return this.GetIdentifier();
            }
            else if (Current == '\n') {
                line++;
                return new Token(TokenKind.Whitespace, Location, Current.ToString());
            }
            else if (char.IsWhiteSpace(Current)) {
                return new Token(TokenKind.Whitespace, Location, Current.ToString());
            }           
            else {
                throw ParsingErrors.UnexpectedCharacter(Location, Current);
            }
        }

        public Token GetToken(IdentifierPath scope) {
            this.scope = scope;

            while (pos < this.text.Length) {
                var tok = this.GetTokenHelper();
                pos++;

                if (tok.Kind != TokenKind.Whitespace) {
                    return tok;
                }
            }

            return new Token(
                TokenKind.EOF, 
                new TokenLocation(pos, 0, line, scope),
                string.Empty);
        }

        public Token PeekToken(IdentifierPath scope) {
            int oldPos = this.pos;
            int oldLine = this.line;
            var tok = this.GetToken(scope);

            this.pos = oldPos;
            this.line = oldLine;

            return tok;
        }
    }
}