using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12 {
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
            else if (this.Text[this.pos] == '/') {
                this.pos++;

                if (this.pos < this.Text.Length && this.Text[this.pos] == '/') {
                    this.pos++;
                    return TokenKind.StrictDivisionSign;
                }
                else {
                    return TokenKind.RealDivisionSign;
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
                return TokenKind.NotSign;
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
                return TokenKind.LessThanSign;
            }
            else if (this.Text[this.pos] == '.') {
                this.pos++;
                return TokenKind.Dot;
            }
            else if (this.Text[pos] == '|') {
                this.pos++;

                if (this.pos < this.Text.Length && this.Text[pos] == '>') {
                    this.pos++;
                    return TokenKind.PipeRight;
                }
                else {
                    throw new Exception($"Unexpected character '{this.Text[pos]}'");
                }
            }
            else if (this.Text[this.pos] == '#') {
                this.pos++;

                if (this.pos < this.Text.Length && this.Text[this.pos] == '#') {
                    pos++; 

                    // Multiline comment
                    while (this.pos < this.Text.Length) {
                        this.pos++;

                        if (this.Text[this.pos] == '#' && this.pos + 1 < this.Text.Length && this.Text[this.pos + 1] == '#') {
                            break;
                        }
                    }
                }
                else {
                    // Single line comment
                    while (this.pos < this.Text.Length && this.Text[this.pos] != '\n' && this.Text[this.pos] != '#') {
                        this.pos++;
                    }
                }

                this.pos++;
                return this.NextToken();
            }
            else if (char.IsDigit(this.Text[this.pos]) || this.Text[this.pos] == '.') {
                string strNum = "";

                while (this.pos < this.Text.Length && (char.IsDigit(this.Text[this.pos]) || this.Text[this.pos] == '.')) {
                    if (this.Text[pos] == '.' && (this.pos >= this.Text.Length || !char.IsDigit(this.Text[pos + 1]))) {
                        break;
                    }

                    strNum += this.Text[this.pos];
                    this.pos++;
                }

                if (long.TryParse(strNum, out long num)) {
                    return new Token<long>(num, TokenKind.Int32Token);
                }
                else if (double.TryParse(strNum, out double floatnum)) {
                    return new Token<double>(floatnum, TokenKind.Real32Token);
                }
                else {
                    throw new Exception();
                }
            }
            else if (char.IsLetterOrDigit(this.Text[this.pos])) {
                string id = "";

                while (this.pos < this.Text.Length && (char.IsLetterOrDigit(this.Text[this.pos])) || this.Text[this.pos] == '_') {
                    id += this.Text[this.pos];
                    this.pos++;
                }
                if (id == "let") {
                    return TokenKind.LetKeyword;
                }
                else if (id == "var") {
                    return TokenKind.VarKeyword;
                }
                else if (id == "function") {
                    return TokenKind.FunctionKeyword;
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
                    return new Token<bool>(true, TokenKind.BooleanLiteral);
                }
                else if (id == "false") {
                    return new Token<bool>(false, TokenKind.BooleanLiteral);
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
                else if (id == "box") {
                    return TokenKind.BoxKeyword;
                }
                else if (id == "unbox") {
                    return TokenKind.UnboxKeyword;
                }
                else if (id == "evoke") {
                    return TokenKind.EvokeKeyword;
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