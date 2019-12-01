using System;

namespace JoshuaKearney.Attempt15.Parsing {
    public class Lexer {
        private int pos = 0;

        public string Text { get; }

        public Lexer(string text) {
            this.Text = text;
        }

        public void Reset() {
            this.pos = 0;
        }

        public Token NextToken() {
            if (this.pos >= this.Text.Length) {
                return TokenKind.EOF;
            }

            if (this.Text[this.pos] == '*') {
                this.pos++;
                return TokenKind.MultiplicationSign;
            }
            else if (this.Text[this.pos] == '^') {
                this.pos++;
                return TokenKind.ExponentSign;
            }
            else if (this.Text[this.pos] == '/') {
                this.pos++;

                if (this.pos < this.Text.Length && this.Text[this.pos] == '/') {
                    this.pos++;
                    return TokenKind.StrictDivisionSign;
                }
                else {
                    return TokenKind.DivisionSign;
                }
            }
            else if (this.Text[this.pos] == '+') {
                this.pos++;
                return TokenKind.AdditionSign;
            }
            else if (this.Text[this.pos] == '-') {
                this.pos++;

                if (this.pos < this.Text.Length && this.Text[this.pos] == '>') {
                    this.pos++;
                    return TokenKind.Arrow;
                }
                else {
                    return TokenKind.SubtractionSign;
                }
            }
            else if (this.Text[this.pos] == '~') {
                this.pos++;
                return TokenKind.NotKeyword;
            }
            else if (this.Text[this.pos] == '(') {
                this.pos++;
                return TokenKind.OpenParenthesis;
            }
            else if (this.Text[this.pos] == ')') {
                this.pos++;
                return TokenKind.CloseParenthesis;
            }
            else if (this.Text[this.pos] == '{') {
                this.pos++;
                return TokenKind.OpenBrace;
            }
            else if (this.Text[this.pos] == '}') {
                this.pos++;
                return TokenKind.CloseBrace;
            }
            else if (this.Text[this.pos] == ';') {
                this.pos++;
                return TokenKind.Semicolon;
            }
            else if (this.Text[this.pos] == ',') {
                this.pos++;
                return TokenKind.Comma;
            }
            else if (this.Text[this.pos] == '|') {
                this.pos++;
                return TokenKind.Pipe;
            }
            else if (this.Text[this.pos] == ':') {
                this.pos++;
                return TokenKind.Colon;
            }
            else if (this.Text[this.pos] == '=') {
                this.pos++;
                return TokenKind.EqualsSign;
            }
            else if (this.Text[this.pos] == '>') {
                this.pos++;
                return TokenKind.GreaterThanSign;
            }
            else if (this.Text[this.pos] == '<') {
                this.pos++;

                if (this.pos < this.Text.Length && this.Text[this.pos] == '=') {
                    if (this.pos + 1 < this.Text.Length && this.Text[this.pos + 1] == '>') {
                        this.pos += 2;
                        return TokenKind.SpaceshipSign;
                    }
                }

                return TokenKind.LessThanSign;
            }
            else if (this.Text[this.pos] == '#') {
                this.pos++;

                if (this.Text[this.pos] == '#') {
                    this.pos++;

                    // Double line comments
                    while (this.pos < this.Text.Length) {
                        if (this.Text[this.pos] == '#') {
                            this.pos++;

                            if (this.Text[this.pos] == '#') {
                                this.pos++;
                                break;
                            }
                        }

                        this.pos++;
                    }

                    return this.NextToken();
                }
                else {
                    // Single line comments
                    while (this.pos < this.Text.Length && this.Text[this.pos] != '\n' && this.Text[this.pos] != '#') {
                        this.pos++;
                    }

                    this.pos++;
                    return this.NextToken();
                }                
            }
            else if (char.IsDigit(this.Text[this.pos]) || this.Text[this.pos] == '.') {
                string strNum = "";

                while (this.pos < this.Text.Length && (char.IsDigit(this.Text[this.pos]) || this.Text[this.pos] == '.')) {
                    strNum += this.Text[this.pos];
                    this.pos++;
                }

                if (long.TryParse(strNum, out long num)) {
                    return new Token<long>(num, TokenKind.Int);
                }
                else if (double.TryParse(strNum, out double floatnum)) {
                    return new Token<double>(floatnum, TokenKind.Real);
                }
                else {
                    throw new Exception();
                }
            }
            else if (char.IsLetterOrDigit(this.Text[this.pos])) {
                string id = "";

                while (this.pos < this.Text.Length && char.IsLetterOrDigit(this.Text[this.pos])) {
                    id += this.Text[this.pos];
                    this.pos++;
                }

                if (id == "let") {
                    return TokenKind.LetKeyword;
                }
                if (id == "var") {
                    return TokenKind.VarKeyword;
                }
                if (id == "set") {
                    return TokenKind.SetKeyword;
                }
                if (id == "to") {
                    return TokenKind.ToKeyword;
                }
                else if (id == "function") {
                    return TokenKind.FunctionKeyword;
                }
                else if (id == "evoke") {
                    return TokenKind.EvokeKeyword;
                }
                else if (id == "and") {
                    return TokenKind.AndKeyword;
                }
                else if (id == "or") {
                    return TokenKind.OrKeyword;
                }
                else if (id == "xor") {
                    return TokenKind.XorKeyword;
                }
                else if (id == "true") {
                    return new Token<bool>(true, TokenKind.Boolean);
                }
                else if (id == "false") {
                    return new Token<bool>(false, TokenKind.Boolean);
                }
                else if (id == "if") {
                    return TokenKind.IfKeyword;
                }
                else if (id == "then") {
                    return TokenKind.ThenKeyword;
                }
                else if (id == "else") {
                    return TokenKind.ElseKeyword;
                }
                else if (id == "type") {
                    return TokenKind.TypeKeyword;
                }
                else {
                    return new Token<string>(id, TokenKind.Identifier);
                }
            }
            else if (!char.IsWhiteSpace(this.Text[this.pos])) {
                throw new Exception();
            }
            else {
                this.pos++;
                return this.NextToken();
            }
        }
    }
}