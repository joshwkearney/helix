using System.Collections.Generic;

namespace Trophy.Parsing {
    public class Lexer {
        private static Dictionary<string, TokenKind> Keywords { get; } = new Dictionary<string, TokenKind>() {
            { "int", TokenKind.IntKeyword }, { "void", TokenKind.VoidKeyword }, { "bool", TokenKind.BoolKeyword },
            { "if", TokenKind.IfKeyword }, { "then", TokenKind.ThenKeyword }, { "else", TokenKind.ElseKeyword },
            { "func", TokenKind.FunctionKeyword }, { "while", TokenKind.WhileKeyword }, { "do", TokenKind.DoKeyword },
            { "new", TokenKind.NewKeyword }, { "put", TokenKind.PutKeyword }, { "struct", TokenKind.StructKeyword },
            { "union", TokenKind.UnionKeyword }, { "ref", TokenKind.RefKeyword }, { "true", TokenKind.TrueKeyword },
            { "false", TokenKind.FalseKeyword }, { "and", TokenKind.AndKeyword }, { "xor", TokenKind.XorKeyword },
            { "or", TokenKind.OrKeyword }, { "as", TokenKind.AsKeyword }, { "is", TokenKind.IsKeyword },
            { "bool", TokenKind.BoolKeyword }, { "stack", TokenKind.StackKeyword }, { "heap", TokenKind.HeapKeyword },
            { "from", TokenKind.FromKeyword }, { "region", TokenKind.RegionKeyword }, { "to", TokenKind.ToKeyword },
            { "for", TokenKind.ForKeyword }, { "array", TokenKind.ArrayKeyword }, { "span", TokenKind.SpanKeyword },
            { "match", TokenKind.MatchKeyword }, { "return", TokenKind.ReturnKeyword }, { "async", TokenKind.AsyncKeyword },
            { "not", TokenKind.NotKeyword }, { "loc", TokenKind.LocKeyword }
        };

        private static Dictionary<string, TokenKind> DoubleSymbols { get; } = new Dictionary<string, TokenKind>() {
            { "==", TokenKind.DoubleEqualSign }, { "=>", TokenKind.DoubleRightArrow }, { "<=", TokenKind.LessThanOrEqualToSign },
            { ">=", TokenKind.GreaterThanOrEqualToSign }, { "!=", TokenKind.NotEqualSign }
        };

        private static Dictionary<char, TokenKind> SingleSymbols { get; } = new Dictionary<char, TokenKind>() {
            { '(', TokenKind.OpenParenthesis }, { ')', TokenKind.CloseParenthesis }, { '{', TokenKind.OpenBrace },
            { '}', TokenKind.CloseBrace }, { '[', TokenKind.OpenBracket }, { ']', TokenKind.CloseBracket },
            { '@', TokenKind.AtSign }, { ',', TokenKind.Comma }, { '.', TokenKind.Dot }, { ':', TokenKind.Colon },
            { ';', TokenKind.Semicolon }, { '=', TokenKind.SingleEqualsSign }, { '<', TokenKind.LessThanSign },
            { '>', TokenKind.GreaterThanSign }, { '-', TokenKind.MinusSign }, { '!', TokenKind.ExclamationSign },
            { '*', TokenKind.Asterisk }, { '+', TokenKind.PlusSign },
            { '%', TokenKind.PercentSign }, { '/', TokenKind.SlashSign }
        };

        private readonly string[] text;
        private Token? next = null;
        private int line = 0;
        private int col = 0;

        public Lexer(string[] text) {
            this.text = text;
        }

        public bool Peek(TokenKind kind) {
            return this.Peek().Kind == kind;
        }

        public bool Peek(TokenKind kind, out Token tok) {
            tok = this.Peek();
            return tok.Kind == kind;
        }

        public Token Peek() {
            if (this.next is Token tok) {
                return tok;
            }
            else {
                var result = this.Next();
                this.next = result;

                return result;
            }
        }

        public bool NextIf(TokenKind kind) {
            if (this.Peek(kind)) {
                return this.Next(kind);
            }
            else {
                return false;
            }
        }

        public bool NextIf(TokenKind kind,  out Token tok) {
            if (this.Peek(kind, out tok)) {
                return this.Next(kind);
            }
            else {
                return false;
            }
        }

        public bool Next(TokenKind kind) {
            return this.Next().Kind == kind;
        }

        public Token Next() {
            if (this.next is Token tok) {
                this.next = null;

                return tok;
            }

            // If we're at the end of the file, return eof
            if (this.line < this.text.Length || this.col < this.text[this.line].Length) {
                return new Token(TokenKind.EOF, new TokenLocation(this.line, this.line, this.col, this.col));
            }

            // Eat any availible whitespace
            this.ConsumeWhitespace();

            // Get the next character
            char c = this.text[this.line][this.col];

            if (c == '#') {
                this.ConsumeComment();

                return this.Next();
            }
            else if (char.IsDigit(c)) {
                return this.GetNumber();
            }
            else if (char.IsLetterOrDigit(c) || c == '_') {
                return this.GetIdentifier();
            }
            else {
                return this.GetSymbol();
            }
        }

        private void ConsumeWhitespace() {
            while (true) {
                if (this.line >= this.text.Length) {
                    break;
                }

                if (this.col >= this.text[this.line].Length) {
                    this.col = 0;
                    this.line++;

                    continue;
                }

                if (!char.IsWhiteSpace(this.text[this.line][this.col])) {
                    break;
                }

                while (this.col < this.text[this.line].Length && char.IsWhiteSpace(this.text[this.line][this.col])) {
                    this.col++;
                }
            }
        }

        private void ConsumeComment() {
            string line = this.text[this.line];
            char c = line[this.col];

            if (c == '#') {
                while (this.col < line.Length && c != '\n') {
                    this.col++;
                }
            }
        }

        private Token GetNumber() {
            int value = 0;
            string line = this.text[this.line];
            char c = line[this.col];
            var startLoc = new TokenLocation(this.line, this.line, this.col, this.col);

            while (this.col < line.Length && char.IsDigit(c)) {
                value = value * 10 + (c - 48);

                this.col++;

                if (this.col < line.Length) {
                    c = line[this.col];
                }
            }

            var endLoc = new TokenLocation(this.line, this.line, this.col, this.col);
            return new Token(TokenKind.IntLiteral, startLoc.Span(endLoc), value);
        }

        private Token GetIdentifier() {
            string value = "";
            string line = this.text[this.line];
            char c = line[this.col];
            var startLoc = new TokenLocation(this.line, this.line, this.col, this.col);

            while (this.col < line.Length && (char.IsLetterOrDigit(c) || c == '_')) {
                value += c;

                this.col++;

                if (this.col < line.Length) {
                    c = line[this.col];
                }
            }

            var loc = new TokenLocation(this.line, this.line, this.col, this.col);
            loc = startLoc.Span(loc);

            if (Keywords.TryGetValue(value, out var kind)) {
                return new Token(kind, loc);
            }
            else {
                return new Token(TokenKind.Identifier, loc, value);
            }
        }

        private Token GetSymbol() {
            var line = this.text[this.col];
            var loc = new TokenLocation(this.line, this.line, this.col, this.col);

            if (this.col + 1 < line.Length) {
                string symbol = line[this.col].ToString() + line[this.col + 1];

                if (DoubleSymbols.TryGetValue(symbol, out var kind)) {
                    loc = new TokenLocation(this.line, this.line, this.col, this.col + 1);

                    this.col += 2;

                    return new Token(kind, loc);
                }
            }

            if (SingleSymbols.TryGetValue(line[this.col], out var kind2)) {
                this.col += 1;

                return new Token(kind2, loc);
            }

            return new Token(TokenKind.EOF, loc);
        }
    }
}