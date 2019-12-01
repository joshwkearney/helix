using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt13 {
    public enum TokenKind {
        Int32Token,
        OpenBracket,
        CloseBracket,
        OpenBrace,
        CloseBrace,
        Comma,
        Symbol,
        Pipe,
        Colon,
        EOF
    }

    public class Token {
        public TokenKind Kind { get; }

        public Token(TokenKind kind) {
            this.Kind = kind;
        }

        public override string ToString() {
            return this.Kind.ToString();
        }

        public static implicit operator Token(TokenKind kind) {
            return new Token(kind);
        }
    }

    public class Token<T> : Token {
        public T Value { get; }

        public Token(T value, TokenKind kind) : base(kind) {
            this.Value = value;
        }

        public override string ToString() {
            return $"{this.Kind}({this.Value})";
        }
    }
}