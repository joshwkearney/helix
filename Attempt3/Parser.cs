using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Attempt3 {
    public class Parser {
        private readonly string text;
        private int position;

        public Parser(string text) {
            this.text = Regex.Replace(text, @"\s+", "");
        }

        public ISyntaxTree Tokenize() {
            this.position = 0;
            return this.Expression();
        }

        private bool HasNextChar() {
            return this.position < this.text.Length;
        }

        private bool HasNextChar(Func<char, bool> predicate) {
            if (!this.HasNextChar()) {
                return false;
            }
            else {
                return predicate(this.text[this.position]);
            }
        } 

        private char PeekChar() {
            if (!this.HasNextChar()) {
                throw new CompileException(CompileExceptionCategory.Syntactic, this.position, "Unexpected end of file");
            }

            return this.text[this.position];
        }

        private char NextChar() {
            char next = this.PeekChar();
            this.position++;
            return next;
        }

        private char NextChar(char expected) {
            char next = this.PeekChar();

            if (next != expected) {
                throw new CompileException(CompileExceptionCategory.Syntactic, this.position, $"Unexpected character. Expected '{expected}', found '{next}'");
            }

            this.position++;
            return next;
        }

        private char NextChar(IEnumerable<char> expected) {
            char next = this.PeekChar();

            if (expected.Any(x => x == next)) {
                throw new CompileException(CompileExceptionCategory.Syntactic, this.position, $"Unexpected character. Expected one of '{string.Join(", ", expected)}', found '{next}'");
            }

            this.position++;
            return next;
        }

        private bool HasIdentifier() {
            return this.HasNextChar() && char.IsLetter(this.PeekChar());
        }

        private string PeekIdentifier() {
            if (!this.HasIdentifier()) {
                char next = this.NextChar();
                throw new CompileException(CompileExceptionCategory.Syntactic, this.position, $"Expected identifier, got {next}");
            }

            string current = "";
            int pos = this.position;
            while (pos < this.text.Length && char.IsLetter(this.text[pos])) {
                current += this.text[pos++];
            }

            return current;
        }

        public string NextIdentifier() {
            string id = this.PeekIdentifier();
            this.position += id.Length;

            return id;
        }

        public ISyntaxTree Integer() {
            string strNum = "";

            while (this.HasNextChar(char.IsDigit)) {
                strNum += this.NextChar();
            }

            if (!int.TryParse(strNum, out var num)) {
                throw new CompileException(CompileExceptionCategory.Semantic, this.position, "Invalid integer");
            }

            return new IntegerLiteral(num);
        }

        public ISyntaxTree Atom() {
            if (this.PeekChar() == '(') {
                this.NextChar();
                var result = this.Expression();
                this.NextChar(')');

                return result;
            }
            else if (this.PeekChar() == '+') {
                this.NextChar();
                return this.Atom();
            }
            else if (this.PeekChar() == '-') {
                this.NextChar();
                return new FunctionCallExpression(new IdentifierLiteral("SubtractInt32"), new IntegerLiteral(0), this.Atom());
            }
            else if (this.HasIdentifier()) {
                return new IdentifierLiteral(this.NextIdentifier());
            }
            else {
                return this.Integer();
            }
        }

        public ISyntaxTree FunctionCall() {
            var first = this.Atom();

            while (this.HasNextChar() && this.PeekChar() == '(') {
                this.NextChar('(');

                List<ISyntaxTree> list = new List<ISyntaxTree>();

                while (this.PeekChar() != ')') {
                    list.Add(this.Expression());

                    if (this.PeekChar() == ',') {
                        this.NextChar(',');
                    }
                }
                this.NextChar(')');

                first = new FunctionCallExpression(first, list.ToArray());
            }

            return first;
        }

        public ISyntaxTree MultiplyExpression() {
            var first = this.FunctionCall();

            while (this.HasNextChar() && (this.PeekChar() == '*' || this.PeekChar() == '/')) {
                char next = this.NextChar();
                string name = "";

                if (next == '*') {
                    name = "MultiplyInt32";
                }
                else if (next == '/') {
                    name = "DivideInt32";
                }
                else {
                    throw new CompileException(CompileExceptionCategory.Syntactic, this.position, $"Invalid multiplication operator {next}");
                }

                first = new FunctionCallExpression(new IdentifierLiteral(name), first, this.FunctionCall());
            }

            return first;
        }

        public ISyntaxTree AddExpression() {
            var first = this.MultiplyExpression();

            while (this.HasNextChar() && (this.PeekChar() == '+' || this.PeekChar() == '-')) {
                char next = this.NextChar();
                string name = "";

                if (next == '+') {
                    name = "AddInt32";
                }
                else if (next == '-') {
                    name = "SubtractInt32";
                }
                else {
                    throw new CompileException(CompileExceptionCategory.Syntactic, this.position, $"Invalid addition operator {next}");
                }

                first = new FunctionCallExpression(new IdentifierLiteral(name), first, this.MultiplyExpression());
            }

            return first;
        }

        public ISyntaxTree Expression() {
            return this.AddExpression();
        }
    }
}