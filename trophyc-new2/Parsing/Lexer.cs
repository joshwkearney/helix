namespace Trophy.Parsing {
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
            { "xor", TokenKind.XorKeyword }, { "put", TokenKind.PutKeyword }
        };

        private static readonly Dictionary<char, TokenKind> symbols = new() {
            { '(', TokenKind.OpenParenthesis }, { ')', TokenKind.CloseParenthesis },
            { '{', TokenKind.OpenBrace }, { '}', TokenKind.CloseBrace },
            { '[', TokenKind.OpenBracket }, { ']', TokenKind.CloseBracket },
            { ',', TokenKind.Comma }, { '.', TokenKind.Dot },
            { ';', TokenKind.Semicolon }, { '+', TokenKind.Add },
            { '-', TokenKind.Subtract }, { '*', TokenKind.Multiply },
            { '%', TokenKind.Modulo }, { '^', TokenKind.Caret }
        };

        private readonly string text;

        private int pos = 0;
        private int line = 1;

        private char Current => this.text[this.pos];

        private TokenLocation Location => new(pos, 1, this.line);

        public Lexer(string text) {
            this.text = text;
        }

        private Token GetLessThanOrArrowOrLessThanOrEqualTo() {
            if (pos + 1 < this.text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.LessThanOrEqualTo, new TokenLocation(pos - 1, 2, this.line), "<=");
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

                    return new Token(TokenKind.Yields, new TokenLocation(pos - 1, 2, line), "=>");
                }
                else if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.Equals, new TokenLocation(pos - 1, 2, line), "==");
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

                    return new Token(TokenKind.GreaterThanOrEqualTo, new TokenLocation(pos - 1, 2, line), ">=");
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

                    return new Token(TokenKind.NotEquals, new TokenLocation(pos - 1, 2, line), "!=");
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

            var loc = new TokenLocation(start, strNum.Length, line);

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
            if (pos >= this.text.Length || !char.IsLetterOrDigit(Current)) {
                throw ParsingErrors.UnexpectedCharacter(Location, Current);
            }

            int c = (int)Current;

            // Advance past the second '
            pos++;
            if (pos >= this.text.Length || Current != '\'') {
                throw ParsingErrors.UnexpectedCharacter(Location, Current);
            }

            return new Token(TokenKind.IntLiteral, new TokenLocation(start, 3, line), c.ToString());
        }


        private Token GetSlashOrComment() {
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
            }

            return new Token(TokenKind.Divide, Location, "/");            
        }

        private Token GetToken() {
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
            else if (Current == '/') {
                return this.GetSlashOrComment();
            }
            else {
                throw ParsingErrors.UnexpectedCharacter(Location, Current);
            }
        }

        public IReadOnlyList<Token> GetTokens() {
            var list = new List<Token>();

            while (pos < this.text.Length) {
                var tok = this.GetToken();

                if (tok.Kind != TokenKind.Whitespace) {
                    list.Add(tok);
                }

                pos++;
            }

            return list;
        }
    }
}