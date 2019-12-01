using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt12.DataFormat {
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
            var list = new List<Data>() { this.Atom() };

            while (this.Peek(TokenKind.Colon)) {
                this.Advance(TokenKind.Colon);
                list.Add(this.Atom());
            }

            if (list.Count == 1) {
                return list[0];
            }
            else {
                list.Reverse();
                return list.Aggregate((x, y) => Data.From(
                    new Dictionary<Data, Data>() {
                        { y, x }
                    }    
                ));                
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
                this.Advance(TokenKind.OpenBrace);
                return this.DictionaryExpression(true);
            }
            else if (this.Peek(TokenKind.DoubleOpenBrace)) {
                this.Advance(TokenKind.DoubleOpenBrace);
                return this.DictionaryExpression(false);
            }
            else if (this.Peek(TokenKind.OpenBracket)) {
                this.Advance(TokenKind.OpenBracket);
                return this.ListExpression(true);
            }
            else if (this.Peek(TokenKind.DoubleOpenBracket)) {
                this.Advance(TokenKind.DoubleOpenBracket);
                return this.ListExpression(false);
            }
            else {
                throw new Exception();
            }
        }

        private Data ListExpression(bool close) {
            List<Data> list = new List<Data>();

            while (!this.Peek(TokenKind.CloseBracket) && !this.Peek(TokenKind.CloseBrace) && !this.Peek(TokenKind.EOF)) {
                list.Add(this.Expression());
            }

            if (close) {
                this.Advance(TokenKind.CloseBracket);
            }

            return Data.From(list);
        }

        private Data DictionaryExpression(bool close) {
            Dictionary<Data, Data> dict = new Dictionary<Data, Data>();

            while (!this.Peek(TokenKind.CloseBracket) && !this.Peek(TokenKind.CloseBrace) && !this.Peek(TokenKind.EOF)) {
                var values = this.Expression().AsDictionary();

                foreach (var pair in values) {
                    dict.Add(pair.Key, pair.Value);
                }
            }

            if (close) {
                this.Advance(TokenKind.CloseBrace);
            }

            return Data.From(dict);
        }
    }
}