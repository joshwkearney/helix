using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt2.Lexing {
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
                return Token.EOF;
            }

            if (this.Text[this.pos] == '*') {
                this.pos++;
                return Token.MultiplicationSign;
            }
            else if (this.Text[this.pos] == '/') {
                this.pos++;
                return Token.DivisionSign;
            }
            else if (this.Text[this.pos] == '+') {
                this.pos++;
                return Token.AdditionSign;
            }
            else if (this.Text[this.pos] == '-') {
                this.pos++;
                return Token.SubtractionSign;
            }
            else if (this.Text[this.pos] == '(') {
                this.pos++;
                return Token.OpenParenthesis;
            }
            else if (this.Text[this.pos] == ')') {
                this.pos++;
                return Token.CloseParenthesis;
            }
            else if (this.Text[this.pos] == '{') {
                this.pos++;
                return Token.OpenBrace;
            }
            else if (this.Text[this.pos] == '}') {
                this.pos++;
                return Token.CloseBrace;
            }
            else if (this.Text[this.pos] == '=') {
                this.pos++;
                return Token.EqualSign;
            }
            else if (char.IsDigit(this.Text[this.pos])) {
                string strNum = "";

                while (this.pos < this.Text.Length && char.IsDigit(this.Text[this.pos])) {
                    strNum += this.Text[this.pos];
                    this.pos++;
                }

                if (int.TryParse(strNum, out int num)) {
                    return new IntToken(num);
                }
                else {
                    CompilerErrors.InvalidIntegerFormat(strNum);
                    return null;
                }
            }
            else if (char.IsLetterOrDigit(this.Text[this.pos])) {
                string id = "";

                while (this.pos < this.Text.Length && char.IsLetterOrDigit(this.Text[this.pos])) {
                    id += this.Text[this.pos];
                    this.pos++;
                }
                if (id == "let") {
                    return Token.LetKeyword;
                }
                else if (id == "if") {
                    return Token.IfKeyword;
                }
                else if (id == "then") {
                    return Token.ThenKeyword;
                }
                else if (id == "else") {
                    return Token.ElseKeyword;
                }
                else if (id == "true") {
                    return new BoolToken(true);
                }
                else if (id == "false") {
                    return new BoolToken(false);
                }
                else {
                    return new IdentifierToken(id);
                }
            }
            else if (!char.IsWhiteSpace(this.Text[this.pos])) {
                CompilerErrors.UnexpectedCharacter(this.Text[this.pos]);
                return null;
            }
            else {
                this.pos++;
                return this.NextToken();
            }
        }
    }
}