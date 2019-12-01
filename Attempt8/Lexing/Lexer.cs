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
                return TokenKind.DivisionSign;
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
            else if (char.IsDigit(this.Text[this.pos]) || this.Text[this.pos] == '.') {
                string strNum = "";

                while (this.pos < this.Text.Length && (char.IsDigit(this.Text[this.pos]) || this.Text[this.pos] == '.')) {
                    strNum += this.Text[this.pos];
                    this.pos++;
                }

                if (int.TryParse(strNum, out int num)) {
                    return new Token<int>(num, TokenKind.Int32Token);
                }
                else if (float.TryParse(strNum, out float floatnum)) {
                    return new Token<float>(floatnum, TokenKind.Real32Token);
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
                else if (id == "var") {
                    return TokenKind.VarKeyword;
                }
                else if (id == "function") {
                    return TokenKind.FunctionKeyword;
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