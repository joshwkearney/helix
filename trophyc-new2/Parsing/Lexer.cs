namespace Trophy.Parsing {
    public class Lexer {
        private static readonly Dictionary<string, TokenKind> keywords = new Dictionary<string, TokenKind>() {
            { "var", TokenKind.VarKeyword }, { "let", TokenKind.LetKeyword }, 
            { "func", TokenKind.FunctionKeyword },
            { "int", TokenKind.IntKeyword }, { "void", TokenKind.VoidKeyword },
            { "bool", TokenKind.BoolKeyword }, { "as", TokenKind.AsKeyword },
            { "is", TokenKind.IsKeyword }, { "if", TokenKind.IfKeyword },
            { "then", TokenKind.ThenKeyword }, { "else", TokenKind.ElseKeyword },
            { "while", TokenKind.WhileKeyword }, { "for", TokenKind.ForKeyword },
            { "do", TokenKind.DoKeyword }, { "to", TokenKind.ToKeyword },
            { "struct", TokenKind.StructKeyword }, { "union", TokenKind.UnionKeyword },
            { "and", TokenKind.AndKeyword }, { "or", TokenKind.OrKeyword },
            { "xor", TokenKind.XorKeyword }
        };

        private static readonly Dictionary<char, TokenKind> symbols = new Dictionary<char, TokenKind>() {
            { '(', TokenKind.OpenParenthesis }, { ')', TokenKind.CloseParenthesis },
            { '{', TokenKind.OpenBrace }, { '}', TokenKind.CloseBrace },
            { '[', TokenKind.OpenBracket }, { ']', TokenKind.CloseBracket },
            { ',', TokenKind.Comma }, { '.', TokenKind.Dot },
            { ';', TokenKind.Semicolon }, { '+', TokenKind.Add },
            { '-', TokenKind.Subtract }, { '*', TokenKind.Multiply },
            { '%', TokenKind.Modulo }
        };

        private readonly string text;

        private int pos = 0;

        private char current => this.text[this.pos];

        private TokenLocation location => new TokenLocation(pos, 1);

        public Lexer(string text) {
            this.text = text;
        }

        private Token GetLessThanOrArrowOrLessThanOrEqualTo() {
            if (pos + 1 < this.text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.LessThanOrEqualTo, new TokenLocation(pos - 1, 2), "<=");
                }
                else {
                    return new Token(TokenKind.LessThan, location, "<");
                }
            }
            else {
                throw ParsingErrors.EndOfFile(new TokenLocation(pos, 1));
            }
        }

        private Token GetEqualsOrYieldsOrAssignment() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '>') {
                    pos++;

                    return new Token(TokenKind.Yields, new TokenLocation(pos - 1, 2), "=>");
                }
                else if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.Equals, new TokenLocation(pos - 1, 2), "==");
                }
                else {
                    return new Token(TokenKind.Assignment, location, "=");
                }
            }
            else {
                throw ParsingErrors.EndOfFile(new TokenLocation(pos, 1));
            }
        }

        private Token GetGreaterThanOrGreaterThanOrEqualTo() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.GreaterThanOrEqualTo, new TokenLocation(pos - 1, 2), ">=");
                }
                else {
                    return new Token(TokenKind.GreaterThan, location, ">");
                }
            }
            else {
                throw ParsingErrors.EndOfFile(new TokenLocation(pos, 1));
            }
        }

        private Token GetNotOrNotEqual() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.NotEquals, new TokenLocation(pos - 1, 2), "!=");
                }
                else {
                    return new Token(TokenKind.Not, location, "!");
                }
            }
            else {
                throw ParsingErrors.EndOfFile(new TokenLocation(pos, 1));
            }
        }

        private Token GetNumber() {
            int start = pos;
            string strNum = "";

            while (pos < this.text.Length && char.IsDigit(current)) {
                strNum += this.text[pos];
                pos++;
            }

            pos--;

            var loc = new TokenLocation(start, strNum.Length);

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

            while (pos < this.text.Length && (char.IsLetterOrDigit(current) || current == '_')) {
                id += this.text[pos];
                pos++;
            }

            pos--;

            var location = new TokenLocation(start, id.Length);

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

        private Token GetSlashOrComment() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '/') {
                    int start = pos;

                    while (pos < text.Length && text[pos] != '\n') {
                        pos++;
                    }

                    pos--;

                    var location = new TokenLocation(start, pos - start + 1);
                    return new Token(TokenKind.Whitespace, location, "");
                }
            }

            return new Token(TokenKind.Divide, location, "/");            
        }

        private Token GetToken() {
            if (pos >= text.Length) {
                return new Token(TokenKind.EOF, new TokenLocation(), "");
            }

            if (symbols.TryGetValue(current, out var kind)) {
                return new Token(kind, location, current.ToString());
            }

            
            if (current == '=') {
                return this.GetEqualsOrYieldsOrAssignment();
            }
            else if (current == '<') {
                return this.GetLessThanOrArrowOrLessThanOrEqualTo();
            }
            else if (current == '>') {
                return this.GetGreaterThanOrGreaterThanOrEqualTo();
            }
            else if (current == '!') {
                return this.GetNotOrNotEqual();
            }            
            else if (char.IsDigit(current)) {
                return this.GetNumber();
            }
            else if (char.IsLetter(current)) {
                return this.GetIdentifier();
            }
            else if (char.IsWhiteSpace(current)) {
                return new Token(TokenKind.Whitespace, location, current.ToString());
            }
            else if (current == '/') {
                return this.GetSlashOrComment();
            }
            else {
                throw ParsingErrors.UnexpectedCharacter(location, current);
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