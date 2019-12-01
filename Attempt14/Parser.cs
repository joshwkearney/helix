using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt14 {
    public class Parser {
        private Lexer lexer;

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

            if (this.Peek(TokenKind.Colon)) {
                this.Advance(TokenKind.Colon);

                return Data.FromDictionary(new Dictionary<Data, Data>() {
                    { key, this.Expression() }
                });
            }
            else if (this.Peek(TokenKind.Pipe)) {
                List<Data> list = new List<Data>();
                this.Advance(TokenKind.Pipe);

                while (!this.Peek(TokenKind.CloseBracket) && !this.Peek(TokenKind.CloseBrace) && !this.Peek(TokenKind.EOF)) {
                    list.Add(this.Expression());
                }

                if (this.Peek(TokenKind.CloseBracket)) {
                    this.Advance(TokenKind.CloseBracket);
                }

                return Data.FromDictionary(new Dictionary<Data, Data>() {
                    { key, Data.FromList(list) }
                });
            }
            else {
                return key;
            }
        }

        private Data Atom() {
            if (this.Peek() is Token<int> intTok) {
                this.Advance();
                return Data.FromInteger(intTok.Value);
            }
            else if (this.Peek(TokenKind.Symbol)) {
                string name = this.Advance<string>();
                return Data.FromSymbol(name);
            }
            else if (this.Peek(TokenKind.OpenBrace)) {
                return this.DictionaryExpression();
            }
            else if (this.Peek(TokenKind.OpenBracket)) {
                return this.ListExpression();
            }
            else {
                throw new Exception();
            }
        }

        private Data ListExpression() {
            List<Data> list = new List<Data>();
            this.Advance(TokenKind.OpenBracket);

            while (!this.Peek(TokenKind.CloseBracket) && !this.Peek(TokenKind.CloseBrace) && !this.Peek(TokenKind.EOF)) {
                list.Add(this.Expression());
            }

            this.Advance(TokenKind.CloseBracket);

            return Data.FromList(list);
        }

        private Data DictionaryExpression() {
            Dictionary<Data, Data> dict = new Dictionary<Data, Data>();
            this.Advance(TokenKind.OpenBrace);

            while (!this.Peek(TokenKind.CloseBracket) && !this.Peek(TokenKind.CloseBrace) && !this.Peek(TokenKind.EOF)) {
                var values = this.Expression().AsDictionary();

                foreach (var pair in values) {
                    dict.Add(pair.Key, pair.Value);
                }
            }

            this.Advance(TokenKind.CloseBrace);

            return Data.FromDictionary(dict);
        }
    }
}