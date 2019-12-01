using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt13 {
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

            if (this.Text[this.pos] == '[') {
                this.pos++;

                return TokenKind.OpenBracket;
            }
            else if (this.Text[this.pos] == ']') {
                this.pos++;
                return TokenKind.CloseBracket;
            }
            else if (this.Text[this.pos] == '{') {
                this.pos++;
                return TokenKind.OpenBrace;
            }
            else if (this.Text[this.pos] == '}') {
                this.pos++;
                return TokenKind.CloseBrace;
            }            
            else if (this.Text[this.pos] == '|') {
                this.pos++;
                return TokenKind.Pipe;
            }
            else if (this.Text[this.pos] == ':' || this.Text[this.pos] == '=') {
                this.pos++;
                return TokenKind.Colon;
            }
            else if (this.Text[this.pos] == ',') {
                this.pos++;
                return TokenKind.Comma;
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

                if (int.TryParse(strNum, out int num)) {
                    return new Token<int>(num, TokenKind.Int32Token);
                }
                else {
                    throw new Exception();
                }
            }
            else if (!this.IsCharSyntax(this.Text[this.pos])) {
                string id = "";

                while (this.pos < this.Text.Length && !this.IsCharSyntax(this.Text[this.pos])) {
                    id += this.Text[this.pos];
                    this.pos++;
                }

                return new Token<string>(id, TokenKind.Symbol);
            }
            else if (!char.IsWhiteSpace(this.Text[this.pos])) {
                throw new Exception(this.Text[this.pos].ToString());
            }
            else {
                this.pos++;
                return this.NextToken();
            }
        }

        public bool IsCharSyntax(char c) {
            return char.IsWhiteSpace(c)
                || c == '{' || c == '}'
                || c == '#' || c == '[' || c == ']'
                || c == '|' || c == ':';
        }
    }
}