namespace Helix.Parsing {
    /// <summary>
    /// Represents a lexer that tokenizes the Helix language source code.
    /// </summary>
    public class Lexer {
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
        private char Current => this.text[this.pos];

        /// <summary>
        /// Gets the current location within the source text.
        /// </summary>
        private TokenLocation Location => new(this.pos, 1, this.line);

        /// <summary>
        /// Initializes a new instance of the <see cref="Lexer"/> class.
        /// </summary>
        /// <param name="text">The source text to be tokenized.</param>
        public Lexer(string text) {
            this.text = text;
        }

        private Token GetLessThanOrArrowOrLessThanOrEqualTo() {
            if (this.pos + 1 < this.text.Length) {
                if (this.text[this.pos + 1] == '=') {
                    this.pos++;

                    return new Token(TokenKind.LessThanOrEqualTo, new TokenLocation(this.pos - 1, 2, this.line), "<=");
                }
                else {
                    return new Token(TokenKind.LessThan, this.Location, "<");
                }
            }
            else {
                throw ParseException.EndOfFile(this.Location);
            }
        }

        private Token GetEqualsOrYieldsOrAssignment() {
            if (this.pos + 1 < this.text.Length) {
                if (this.text[this.pos + 1] == '=') {
                    this.pos++;

                    return new Token(TokenKind.Equals, new TokenLocation(this.pos - 1, 2, this.line), "==");
                }
                else {
                    return new Token(TokenKind.Assignment, this.Location, "=");
                }
            }
            else {
                throw ParseException.EndOfFile(this.Location);
            }
        }

        private Token GetGreaterThanOrGreaterThanOrEqualTo() {
            if (this.pos + 1 < this.text.Length) {
                if (this.text[this.pos + 1] == '=') {
                    this.pos++;

                    return new Token(TokenKind.GreaterThanOrEqualTo, new TokenLocation(this.pos - 1, 2, this.line), ">=");
                }
                else {
                    return new Token(TokenKind.GreaterThan, this.Location, ">");
                }
            }
            else {
                throw ParseException.EndOfFile(this.Location);
            }
        }

        private Token GetNotOrNotEqual() {
            if (this.pos + 1 < this.text.Length) {
                if (this.text[this.pos + 1] == '=') {
                    this.pos++;

                    return new Token(TokenKind.NotEquals, new TokenLocation(this.pos - 1, 2, this.line), "!=");
                }
                else {
                    return new Token(TokenKind.Not, this.Location, "!");
                }
            }
            else {
                throw ParseException.EndOfFile(this.Location);
            }
        }

        private Token GetNumber() {
            int start = this.pos;
            string strNum = "";

            while (this.pos < this.text.Length && char.IsDigit(this.Current)) {
                strNum += this.text[this.pos];
                this.pos++;
            }

            this.pos--;

            var loc = new TokenLocation(start, strNum.Length, this.line);

            if (int.TryParse(strNum, out int num)) {
                return new Token(TokenKind.WordLiteral, loc, strNum);
            }
            else {
                throw ParseException.InvalidNumber(loc, strNum);
            }
        }

        private Token GetIdentifier() {
            int start = this.pos;
            string id = "";

            while (this.pos < this.text.Length && (char.IsLetterOrDigit(this.Current) || this.Current == '_')) {
                id += this.text[this.pos];
                this.pos++;
            }

            this.pos--;

            var location = new TokenLocation(start, id.Length, this.line);

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
            int start = this.pos++;

            // Get the character
            if (this.pos >= this.text.Length || !char.IsLetterOrDigit(this.Current)) {
                throw ParseException.UnexpectedCharacter(this.Location, this.Current);
            }

            int c = (int)this.Current;

            // Advance past the second '
            this.pos++;
            if (this.pos >= this.text.Length || this.Current != '\'') {
                throw ParseException.UnexpectedCharacter(this.Location, this.Current);
            }

            return new Token(TokenKind.WordLiteral, new TokenLocation(start, 3, this.line), c.ToString());
        }

        private Token GetSlashOrCommentOrDivideAssignment() {
            if (this.pos + 1 < this.text.Length) {
                if (this.text[this.pos + 1] == '/') {
                    int start = this.pos;

                    while (this.pos < this.text.Length && this.text[this.pos] != '\n') {
                        this.pos++;
                    }

                    this.pos--;

                    var location = new TokenLocation(start, this.pos - start + 1, this.line);
                    return new Token(TokenKind.Whitespace, location, "");
                }
                else if (this.text[this.pos + 1] == '=') {
                    this.pos++;

                    return new Token(
                        TokenKind.DivideAssignment, 
                        new TokenLocation(this.pos - 1, 2, this.line), "/=");
                }
            }

            return new Token(TokenKind.Divide, this.Location, "/");            
        }

        private Token GetPlusOrPlusAssignment() {
            if (this.pos + 1 < this.text.Length) {
                if (this.text[this.pos + 1] == '=') {
                    this.pos++;

                    return new Token(
                        TokenKind.PlusAssignment,
                        new TokenLocation(this.pos - 1, 2, this.line), "+=");
                }
            }

            return new Token(TokenKind.Plus, this.Location, "+");
        }

        private Token GetMinusOrMinusAssignment() {
            if (this.pos + 1 < this.text.Length) {
                if (this.text[this.pos + 1] == '=') {
                    this.pos++;

                    return new Token(
                        TokenKind.MinusAssignment,
                        new TokenLocation(this.pos - 1, 2, this.line), "-=");
                }
                else if (this.text[this.pos + 1] == '>') {
                    this.pos++;

                    return new Token(TokenKind.Yields, new TokenLocation(this.pos - 1, 2, this.line), "->");
                }
            }

            return new Token(TokenKind.Minus, this.Location, "-");
        }

        private Token GetStarOrStarAssignment() {
            if (this.pos + 1 < this.text.Length) {
                if (this.text[this.pos + 1] == '=') {
                    this.pos++;

                    return new Token(
                        TokenKind.StarAssignment,
                        new TokenLocation(this.pos - 1, 2, this.line), "*=");
                }
            }

            return new Token(TokenKind.Star, this.Location, "*");
        }

        private Token GetModuloOrModuloAssignment() {
            if (this.pos + 1 < this.text.Length) {
                if (this.text[this.pos + 1] == '=') {
                    this.pos++;

                    return new Token(
                        TokenKind.ModuloAssignment,
                        new TokenLocation(this.pos - 1, 2, this.line), "%=");
                }
            }

            return new Token(TokenKind.Modulo, this.Location, "%");
        }

        private Token GetTokenHelper() {
            if (this.pos >= this.text.Length) {
                return new Token(TokenKind.EOF, new TokenLocation(), "");
            }

            if (symbols.TryGetValue(this.Current, out var kind)) {
                return new Token(kind, this.Location, this.Current.ToString());
            }

            if (this.Current == '=') {
                return this.GetEqualsOrYieldsOrAssignment();
            }
            else if (this.Current == '<') {
                return this.GetLessThanOrArrowOrLessThanOrEqualTo();
            }
            else if (this.Current == '>') {
                return this.GetGreaterThanOrGreaterThanOrEqualTo();
            }
            else if (this.Current == '!') {
                return this.GetNotOrNotEqual();
            }
            else if (this.Current == '\'') {
                return this.GetCharLiteral();
            }
            else if (this.Current == '+') {
                return this.GetPlusOrPlusAssignment();
            }
            else if (this.Current == '-') {
                return this.GetMinusOrMinusAssignment();
            }
            else if (this.Current == '*') {
                return this.GetStarOrStarAssignment();
            }
            else if (this.Current == '/') {
                return this.GetSlashOrCommentOrDivideAssignment();
            }
            else if (this.Current == '%') {
                return this.GetModuloOrModuloAssignment();
            }
            else if (char.IsDigit(this.Current)) {
                return this.GetNumber();
            }
            else if (char.IsLetter(this.Current)) {
                return this.GetIdentifier();
            }
            else if (this.Current == '\n') {
                this.line++;
                return new Token(TokenKind.Whitespace, this.Location, this.Current.ToString());
            }
            else if (char.IsWhiteSpace(this.Current)) {
                return new Token(TokenKind.Whitespace, this.Location, this.Current.ToString());
            }           
            else {
                throw ParseException.UnexpectedCharacter(this.Location, this.Current);
            }
        }

        /// <summary>
        /// Retrieves the next token from the source text.
        /// </summary>
        /// <returns>The tokenized segment from the source.</returns>
        /// <exception cref="ParseException">Thrown when there's a parsing error.</exception>
        public Token GetToken() {
            while (this.pos < this.text.Length) {
                var tok = this.GetTokenHelper();
                this.pos++;

                if (tok.Kind != TokenKind.Whitespace) {
                    return tok;
                }
            }

            return new Token(
                TokenKind.EOF, 
                new TokenLocation(this.pos, 0, this.line),
                string.Empty);
        }

        /// <summary>
        /// Peeks at the next token from the source text without advancing the position.
        /// </summary>
        /// <returns>The next token from the source.</returns>
        public Token PeekToken() {
            int oldPos = this.pos;
            int oldLine = this.line;
            var tok = this.GetToken();

            this.pos = oldPos;
            this.line = oldLine;

            return tok;
        }
    }
}