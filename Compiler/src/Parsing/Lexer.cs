using System.Collections.Generic;

namespace Trophy.Parsing {
    public class Lexer {
        private readonly string text;

        private int pos = 0;

        private char current => this.text[this.pos];

        private TokenLocation location => new TokenLocation(pos, 1);

        public Lexer(string text) {
            this.text = text;
        }

        private IToken GetLessThanOrArrowOrLessThanOrEqualTo() {

            if (pos + 1 < this.text.Length) {
                if (text[pos + 1] == '-') {
                    pos++;

                    return new Token(TokenKind.LeftArrow, new TokenLocation(pos - 1, 2));
                }
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.LessThanOrEqualToSign, new TokenLocation(pos - 1, 2));
                }
                else {
                    return new Token(TokenKind.LessThanSign, location);
                }
            }
            else {
                throw ParsingErrors.EndOfFile(new TokenLocation(pos, 1));
            }
        }

        private IToken GetEqualsOrYieldsOrAssignment() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '>') {
                    pos++;

                    return new Token(TokenKind.YieldSign, new TokenLocation(pos - 1, 2));
                }
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.EqualSign, new TokenLocation(pos - 1, 2));
                }
                else {
                    return new Token(TokenKind.AssignmentSign, location);
                }
            }
            else {
                throw ParsingErrors.EndOfFile(new TokenLocation(pos, 1));
            }
        }

        private IToken GetGreaterThanOrGreaterThanOrEqualTo() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.GreaterThanOrEqualToSign, new TokenLocation(pos - 1, 2));
                }
                else {
                    return new Token(TokenKind.GreaterThanSign, location);
                }
            }
            else {
                throw ParsingErrors.EndOfFile(new TokenLocation(pos, 1));
            }
        }

        private IToken GetNotOrNotEqual() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '=') {
                    pos++;

                    return new Token(TokenKind.NotEqualSign, new TokenLocation(pos - 1, 2));
                }
                else {
                    return new Token(TokenKind.NotSign, location);
                }
            }
            else {
                throw ParsingErrors.EndOfFile(new TokenLocation(pos, 1));
            }
        }

        private IToken GetMinusOrRightArrow() {
            if (pos + 1 < text.Length) {
                if (text[pos + 1] == '>') {
                    pos++;

                    return new Token(TokenKind.RightArrow, new TokenLocation(pos - 1, 2));
                }
                else {
                    return new Token(TokenKind.SubtractSign, location);
                }
            }
            else {
                throw ParsingErrors.EndOfFile(new TokenLocation(pos, 1));
            }
        }


        private IToken GetNumber() {
            int start = pos;
            string strNum = "";

            while (pos < this.text.Length && char.IsDigit(current)) {
                strNum += this.text[pos];
                pos++;
            }

            pos--;

            var loc = new TokenLocation(start, strNum.Length);

            if (int.TryParse(strNum, out int num)) {
                return new Token<int>(num, TokenKind.IntLiteral, loc);
            }
            else {
                throw ParsingErrors.InvalidNumber(loc, strNum);
            }
        }

        private IToken GetIdentifier() {
            int start = pos;
            string id = "";

            while (pos < this.text.Length && (char.IsLetterOrDigit(current) || current == '_')) {
                id += this.text[pos];
                pos++;
            }

            pos--;

            var location = new TokenLocation(start, id.Length);

            if (id == "var") {
                return new Token(TokenKind.VarKeyword, location);
            }
            else if (id == "int") {
                return new Token(TokenKind.IntKeyword, location);
            }
            else if (id == "void") {
                return new Token(TokenKind.VoidKeyword, location);
            }
            else if (id == "if") {
                return new Token(TokenKind.IfKeyword, location);
            }
            else if (id == "then") {
                return new Token(TokenKind.ThenKeyword, location);
            }
            else if (id == "else") {
                return new Token(TokenKind.ElseKeyword, location);
            }
            else if (id == "func") {
                return new Token(TokenKind.FunctionKeyword, location);
            }
            else if (id == "while") {
                return new Token(TokenKind.WhileKeyword, location);
            }
            else if (id == "do") {
                return new Token(TokenKind.DoKeyword, location);
            }
            else if (id == "new") {
                return new Token(TokenKind.NewKeyword, location);
            }
            else if (id == "struct") {
                return new Token(TokenKind.StructKeyword, location);
            }
            else if (id == "class") {
                return new Token(TokenKind.ClassKeyword, location);
            }
            else if (id == "union") {
                return new Token(TokenKind.UnionKeyword, location);
            }
            else if (id == "ref") {
                return new Token(TokenKind.RefKeyword, location);
            }
            else if (id == "true") {
                return new Token<bool>(true, TokenKind.BoolLiteral, location);
            }
            else if (id == "false") {
                return new Token<bool>(false, TokenKind.BoolLiteral, location);
            }
            else if (id == "and") {
                return new Token(TokenKind.AndKeyword, location);
            }
            else if (id == "xor") {
                return new Token(TokenKind.XorKeyword, location);
            }
            else if (id == "or") {
                return new Token(TokenKind.OrKeyword, location);
            }
            else if (id == "as") {
                return new Token(TokenKind.AsKeyword, location);
            }
            else if (id == "bool") {
                return new Token(TokenKind.BoolKeyword, location);
            }
            else if (id == "stack") {
                return new Token(TokenKind.StackKeyword, location);
            }
            else if (id == "heap") {
                return new Token(TokenKind.HeapKeyword, location);
            }
            else if (id == "alloc") {
                return new Token(TokenKind.AllocKeyword, location);
            }
            else if (id == "from") {
                return new Token(TokenKind.FromKeyword, location);
            }
            else if (id == "region") {
                return new Token(TokenKind.RegionKeyword, location);
            }
            else if (id == "to") {
                return new Token(TokenKind.ToKeyword, location);
            }
            else if (id == "for") {
                return new Token(TokenKind.ForKeyword, location);
            }
            else if (id == "array") {
                return new Token(TokenKind.ArrayKeyword, location);
            }
            else {
                return new Token<string>(id, TokenKind.Identifier, location);
            }
        }

        private IToken GetComment() {
            int start = pos;

            while (pos < text.Length && text[pos] != '\n') {
                pos++;
            }

            pos--;

            var location = new TokenLocation(start, pos - start + 1);
            return new Token(TokenKind.Whitespace, location);
        }

        private IToken GetToken() {
            if (pos >= text.Length) {
                return null;
            }

            if (current == '(') {
                return new Token(TokenKind.OpenParenthesis, location);
            }
            else if (current == ')') {
                return new Token(TokenKind.CloseParenthesis, location);
            }
            else if (current == '{') {
                return new Token(TokenKind.OpenBrace, location);
            }
            else if (current == '}') {
                return new Token(TokenKind.CloseBrace, location);
            }
            else if (current == '[') {
                return new Token(TokenKind.OpenBracket, location);
            }
            else if (current == ']') {
                return new Token(TokenKind.CloseBracket, location);
            }
            else if (current == '@') {
                return new Token(TokenKind.LiteralSign, location);
            }
            else if (current == ',') {
                return new Token(TokenKind.Comma, location);
            }
            else if (current == '.') {
                return new Token(TokenKind.Dot, location);
            }
            else if (current == ':') {
                return new Token(TokenKind.Colon, location);
            }
            else if (current == ';') {
                return new Token(TokenKind.Semicolon, location);
            }
            else if (current == '=') {
                return this.GetEqualsOrYieldsOrAssignment();
            }
            else if (current == '<') {
                return this.GetLessThanOrArrowOrLessThanOrEqualTo();
            }
            else if (current == '>') {
                return this.GetGreaterThanOrGreaterThanOrEqualTo();
            }
            else if (current == '-') {
                return this.GetMinusOrRightArrow();
            }
            else if (current == '!') {
                return this.GetNotOrNotEqual();
            }
            else if (current == '*') {
                return new Token(TokenKind.MultiplySign, location);
            }
            else if (current == '+') {
                return new Token(TokenKind.AddSign, location);
            }
            else if (current == '|') {
                return new Token(TokenKind.Pipe, location);
            }
            else if (current == '%') {
                return new Token(TokenKind.ModuloSign, location);
            }
            else if (char.IsDigit(current)) {
                return this.GetNumber();
            }
            else if (char.IsLetter(current)) {
                return this.GetIdentifier();
            }
            else if (char.IsWhiteSpace(current)) {
                return new Token(TokenKind.Whitespace, location);
            }
            else if (current == '#') {
                return this.GetComment();
            }
            else {
                throw ParsingErrors.UnexpectedCharacter(location, current);
            }
        }

        public IReadOnlyList<IToken> GetTokens() {
            var list = new List<IToken>();

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