using Helix.Common.Tokens;

namespace Helix.Frontend.ParseTree {
    /// <summary>
    /// Represents a lexer that tokenizes the Helix language source code.
    /// </summary>
    internal class Lexer {
        /// <summary>
        /// Dictionary of keywords mapped to their respective token kinds.
        /// </summary>
        private static readonly Dictionary<string, TokenKind> keywords = new() {
            { "var", TokenKind.VarKeyword },
            { "func", TokenKind.FunctionKeyword }, { "extern", TokenKind.ExternKeyword },
            { "word", TokenKind.WordKeyword }, { "void", TokenKind.VoidKeyword },
            { "bool", TokenKind.BoolKeyword }, { "as", TokenKind.AsKeyword },
            { "is", TokenKind.IsKeyword }, { "if", TokenKind.IfKeyword },
            { "then", TokenKind.ThenKeyword }, { "else", TokenKind.ElseKeyword },
            { "while", TokenKind.WhileKeyword }, { "for", TokenKind.ForKeyword },
            { "do", TokenKind.DoKeyword }, { "to", TokenKind.ToKeyword },
            { "struct", TokenKind.StructKeyword }, { "union", TokenKind.UnionKeyword },
            { "and", TokenKind.AndKeyword }, { "or", TokenKind.OrKeyword },
            { "xor", TokenKind.XorKeyword },
            { "break", TokenKind.BreakKeyword }, { "continue", TokenKind.ContinueKeyword },
            { "return", TokenKind.ReturnKeyword }, { "new", TokenKind.NewKeyword },
            { "until", TokenKind.UntilKeyword }
        };
        /// <summary>
        /// Dictionary of symbols mapped to their respective token kinds.
        /// </summary>
        private static readonly Dictionary<char, TokenKind> symbols = new() {
            { '(', TokenKind.OpenParenthesis }, { ')', TokenKind.CloseParenthesis },
            { '{', TokenKind.OpenBrace }, { '}', TokenKind.CloseBrace },
            { '[', TokenKind.OpenBracket }, { ']', TokenKind.CloseBracket },
            { ',', TokenKind.Comma }, { '.', TokenKind.Dot },
            { ';', TokenKind.Semicolon },
            { '^', TokenKind.Caret }, { '&', TokenKind.Ampersand }
        };

        /// <summary>
        /// Source text to be tokenized.
        /// </summary>
        private readonly string text;

        /// <summary>
        /// Current position in the source text.
        /// </summary>
        private int pos = 0;

        /// <summary>
        /// Current line number in the source text.
        /// </summary>
        private int line = 1;

        /// <summary>
        /// Gets the current character from the source text.
        /// </summary>
        private char Current => text[pos];

        /// <summary>
        /// Gets the current location within the source text.
        /// </summary>
        private TokenLocation Location => new(pos, 1, line);

        /// <summary>
        /// Initializes a new instance of the <see cref="Lexer"/> class.
        /// </summary>
        /// <param name="text">The source text to be tokenized.</param>
        public Lexer(string text) {
            this.text = text;
        }

        private Token GetLessThanOrArrowOrLessThanOrEqualTo() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.LessThanOrEqualTo, new TokenLocation(pos - 1, 2, line), "<=");
                }
                else {
                    return new Token(TokenKind.LessThan, Location, "<");
                }
            }
            else {
                throw ParseException.EndOfFile(Location);
            }
        }

        private Token GetEqualsOrYieldsOrAssignment() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.Equals, new TokenLocation(pos - 1, 2, line), "==");
                }
                else {
                    return new Token(TokenKind.Assignment, Location, "=");
                }
            }
            else {
                throw ParseException.EndOfFile(Location);
            }
        }

        private Token GetGreaterThanOrGreaterThanOrEqualTo() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.GreaterThanOrEqualTo, new TokenLocation(pos - 1, 2, line), ">=");
                }
                else {
                    return new Token(TokenKind.GreaterThan, Location, ">");
                }
            }
            else {
                throw ParseException.EndOfFile(Location);
            }
        }

        private Token GetNotOrNotEqual() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.NotEquals, new TokenLocation(pos - 1, 2, line), "!=");
                }
                else {
                    return new Token(TokenKind.Not, Location, "!");
                }
            }
            else {
                throw ParseException.EndOfFile(Location);
            }
        }

        private Token GetNumber() {
            int start = pos;
            string strNum = "";

            while (pos < text.Length && char.IsDigit(Current)) {
                strNum += text[pos];
                pos++;
            }

            pos--;

            var loc = new TokenLocation(start, strNum.Length, line);

            if (int.TryParse(strNum, out int num)) {
                return new Token(TokenKind.WordLiteral, loc, strNum);
            }
            else {
                throw ParseException.InvalidNumber(loc, strNum);
            }
        }

        private Token GetIdentifier() {
            int start = pos;
            string id = "";

            while (pos < text.Length && (char.IsLetterOrDigit(Current) || Current == '_')) {
                id += text[pos];
                pos++;
            }

            pos--;

            var location = new TokenLocation(start, id.Length, line);

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
            if (pos >= text.Length || !char.IsLetterOrDigit(Current)) {
                throw ParseException.UnexpectedCharacter(Location, Current);
            }

            int c = Current;

            // Advance past the second '
            pos++;
            if (pos >= text.Length || Current != '\'') {
                throw ParseException.UnexpectedCharacter(Location, Current);
            }

            return new Token(TokenKind.WordLiteral, new TokenLocation(start, 3, line), c.ToString());
        }

        private Token GetSlashOrCommentOrDivideAssignment() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '/') {
                    int start = pos;

                    while (pos < text.Length && text[pos] != '\n') {
                        pos++;
                    }

                    pos--;

                    var location = new TokenLocation(start, pos - start + 1, line);
                    return new Token(TokenKind.Whitespace, location, "");
                }
                else if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(
                        TokenKind.DivideAssignment,
                        new TokenLocation(pos - 1, 2, line), "/=");
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
                        new TokenLocation(pos - 1, 2, line), "+=");
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
                        new TokenLocation(pos - 1, 2, line), "-=");
                }
                else if (text[pos + 1] == '>') {
                    pos++;

                    return new Token(TokenKind.Yields, new TokenLocation(pos - 1, 2, line), "->");
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
                        new TokenLocation(pos - 1, 2, line), "*=");
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
                        new TokenLocation(pos - 1, 2, line), "%=");
                }
            }

            return new Token(TokenKind.Modulo, Location, "%");
        }

        private Token GetTokenHelper() {
            if (pos >= text.Length) {
                return new Token(TokenKind.EOF, new TokenLocation(0, 0, 0), "");
            }

            if (symbols.TryGetValue(Current, out var kind)) {
                return new Token(kind, Location, Current.ToString());
            }

            if (Current == '=') {
                return GetEqualsOrYieldsOrAssignment();
            }
            else if (Current == '<') {
                return GetLessThanOrArrowOrLessThanOrEqualTo();
            }
            else if (Current == '>') {
                return GetGreaterThanOrGreaterThanOrEqualTo();
            }
            else if (Current == '!') {
                return GetNotOrNotEqual();
            }
            else if (Current == '\'') {
                return GetCharLiteral();
            }
            else if (Current == '+') {
                return GetPlusOrPlusAssignment();
            }
            else if (Current == '-') {
                return GetMinusOrMinusAssignment();
            }
            else if (Current == '*') {
                return GetStarOrStarAssignment();
            }
            else if (Current == '/') {
                return GetSlashOrCommentOrDivideAssignment();
            }
            else if (Current == '%') {
                return GetModuloOrModuloAssignment();
            }
            else if (char.IsDigit(Current)) {
                return GetNumber();
            }
            else if (char.IsLetter(Current)) {
                return GetIdentifier();
            }
            else if (Current == '\n') {
                line++;
                return new Token(TokenKind.Whitespace, Location, Current.ToString());
            }
            else if (char.IsWhiteSpace(Current)) {
                return new Token(TokenKind.Whitespace, Location, Current.ToString());
            }
            else {
                throw ParseException.UnexpectedCharacter(Location, Current);
            }
        }

        /// <summary>
        /// Retrieves the next token from the source text.
        /// </summary>
        /// <returns>The tokenized segment from the source.</returns>
        /// <exception cref="ParseException">Thrown when there's a parsing error.</exception>
        public Token GetToken() {
            while (pos < text.Length) {
                var tok = GetTokenHelper();
                pos++;

                if (tok.Kind != TokenKind.Whitespace) {
                    return tok;
                }
            }

            return new Token(
                TokenKind.EOF,
                new TokenLocation(pos, 0, line),
                string.Empty);
        }

        /// <summary>
        /// Peeks at the next token from the source text without advancing the position.
        /// </summary>
        /// <returns>The next token from the source.</returns>
        public Token PeekToken() {
            int oldPos = pos;
            int oldLine = line;
            var tok = GetToken();

            pos = oldPos;
            line = oldLine;

            return tok;
        }
    }
}