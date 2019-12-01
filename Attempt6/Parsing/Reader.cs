using Attempt6.Lexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt6.Parsing {
    public class Reader {
        private Lexer lexer;
        private int pos = 0;
        private IReadOnlyList<Token> tokens;

        public Reader(Lexer lexer) {
            this.lexer = lexer;
        }

        public ISyntax Read() {
            List<Token> tokens = new List<Token>();

            while (true) {
                var next = this.lexer.NextToken();

                if (next.Kind == TokenKind.EOF) {
                    break;
                }

                tokens.Add(next);
            }

            this.tokens = tokens;
            return this.Atom();
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
                throw new Exception($"Expected '{nameof(T)}', got '{tok}'");
            }
        }

        private ISyntax Dictionary() {
            this.Advance(TokenKind.OpenBrace);
            List<KeyValuePair<ISyntax, ISyntax>> values = new List<KeyValuePair<ISyntax, ISyntax>>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                AssociativeList<ISyntax, ISyntax> syntax = (this.Atom() as ListSyntax)?.List;                
                if (syntax == null) {
                    throw new Exception();
                }

                if (syntax.Count != 1) {
                    throw new Exception();
                }

                var key = syntax.First().Key;
                var val = syntax.First().Value;

                values.Insert(0, new KeyValuePair<ISyntax, ISyntax>(key, val));
                
                if (this.Peek(TokenKind.Comma)) {
                    this.Advance(TokenKind.Comma);
                }
                else {
                    break;
                }
            }

            this.Advance(TokenKind.CloseBrace);

            return new ListSyntax(AssociativeList<ISyntax, ISyntax>.Empty.AppendAll(values));
        }

        private ISyntax List() {
            this.Advance(TokenKind.OpenParenthesis);

            ISyntax next() {
                var item = this.Atom();

                if (this.Peek(TokenKind.CloseParenthesis)) {
                    return item;
                }
                else {
                    return new ListSyntax(new AssociativeList<ISyntax, ISyntax>(item, next()));
                }
            }

            var ret = next();
            this.Advance(TokenKind.CloseParenthesis);

            return ret;
        }

        private ISyntax Atom() {
            ISyntax atom() {
                if (this.Peek(TokenKind.Int32Token)) {
                    return new Int32Literal(this.Advance<int>());
                }
                else if (this.Peek(TokenKind.OpenBrace)) {
                    return this.Dictionary();
                }
                else if (this.Peek(TokenKind.OpenParenthesis)) {
                    return this.List();
                }
                else if (this.Peek(TokenKind.Identifier)) {
                    return new IdentifierSyntax(this.Advance<string>());
                }
                else {
                    throw new Exception();
                }
            }

            var ret = atom();

            if (this.Peek(TokenKind.Colon)) {
                this.Advance(TokenKind.Colon);
                return new ListSyntax(new AssociativeList<ISyntax, ISyntax>(ret, this.Atom()));
            }
            else {
                return ret;
            }
        }
    }
}