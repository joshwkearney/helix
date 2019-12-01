using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt13 {
    public class Parser {
        private readonly Lexer lexer;

        private int pos = 0;
        private IReadOnlyList<Token> tokens;

        public Parser(Lexer lexer) {
            this.lexer = lexer;
        }

        public Data Parse() {
            List<Token> tokens = new List<Token>();

            while (true) {
                var next = this.lexer.NextToken();

                if (next.Kind == TokenKind.EOF) {
                    break;
                }

                tokens.Add(next);
            }

            this.tokens = tokens;
            return this.Expression();
        }

        private Token Peek() {
            if (this.pos >= this.tokens.Count) {
                return TokenKind.EOF;
            }

            return this.tokens[this.pos];
        }

        private bool Peek(TokenKind kind) {
            return this.Peek().Kind == kind;
        }

        private Token Advance() {
            if (this.pos >= this.tokens.Count) {
                return TokenKind.EOF;
            }

            return this.tokens[this.pos++];
        }

        private Token Advance(TokenKind token) {
            if (this.pos >= this.tokens.Count) {
                return TokenKind.EOF;
            }

            if (this.tokens[this.pos].Kind != token) {
                throw new Exception($"Expected '{token}', got '{this.tokens[this.pos]}'");
            }

            return this.tokens[this.pos++];
        }

        private T Advance<T>() {
            if (this.pos >= this.tokens.Count) {
                throw new Exception("Unexpected end of file");
            }

            var tok = this.tokens[this.pos++];
            if (tok is Token<T> t) {
                return t.Value;
            }
            else {
                throw new Exception($"Expected '{typeof(T).Name}', got '{tok}'");
            }
        }
      
        private Data Expression() {
            return this.ImplicitDictionaryExpression();
        }

        private Data ImplicitDictionaryExpression() {
            var key = this.Atom();

            if (this.Peek(TokenKind.Pipe)) {
                this.Advance(TokenKind.Pipe);

                var list = new List<Data>();
                var result = Data.From(key, list);

                while (!this.Peek(TokenKind.CloseBrace) && !this.Peek(TokenKind.CloseBracket) && !this.Peek(TokenKind.EOF)) {
                    if (this.Peek(TokenKind.Pipe)) {
                        this.Advance(TokenKind.Pipe);

                        var last = list[list.Count - 1];
                        var newList = new List<Data>();
                        var newDict = new Dictionary<Data, IList<Data>>() {
                            { last, newList }
                        };

                        list[list.Count - 1] = Data.From(newDict);
                        list = newList;
                    }
                    else {
                        list.Add(this.Atom());
                    }
                }

                if (this.Peek(TokenKind.CloseBracket)) {
                    this.Advance(TokenKind.CloseBracket);
                }

                return result;
            }
            else if (this.Peek(TokenKind.Colon)) {
                this.Advance(TokenKind.Colon);
                return Data.From(key, this.Expression());
            }
            else {
                return key;
            }
        }

        private Data Atom() {
            if (this.Peek() is Token<int> intTok) {
                this.Advance();
                return Data.From(intTok.Value);
            }
            else if (this.Peek(TokenKind.Symbol)) {
                string name = this.Advance<string>();
                return Data.From(name);
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.DictionaryExpression();
            }
            else {
                throw new Exception($"Unexpected token '{this.Peek()}'");
            }
        }

        private Data DictionaryExpression() {
            this.Advance(TokenKind.OpenBrace);

            var dict = new Dictionary<Data, IList<Data>>();
            while (!this.Peek(TokenKind.CloseBrace)) {
                var entry = this.Expression().AsDictionary();

                if (entry.Count != 1) {
                    throw new Exception();
                }

                dict.Add(entry.Keys.First(), entry.Values.First());
            }

            this.Advance(TokenKind.CloseBrace);

            return Data.From(dict);
        }
    }
}