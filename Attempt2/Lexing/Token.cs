using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt2.Lexing {
    public class Token {
        public static Token AdditionSign { get; } = new Token("+");
        public static Token SubtractionSign { get; } = new Token("-");
        public static Token MultiplicationSign { get; } = new Token("*");
        public static Token DivisionSign { get; } = new Token("/");
        public static Token OpenParenthesis { get; } = new Token("(");
        public static Token CloseParenthesis { get; } = new Token(")");
        public static Token OpenBrace { get; } = new Token("{");
        public static Token CloseBrace { get; } = new Token("}");
        public static Token EqualSign { get; } = new Token("=");
        public static Token LetKeyword { get; } = new Token("let");
        public static Token IfKeyword { get; } = new Token("if");
        public static Token ThenKeyword { get; } = new Token("then");
        public static Token ElseKeyword { get; } = new Token("else");
        public static Token TrueKeyword { get; } = new Token("true");
        public static Token FalseKeyword { get; } = new Token("false");
        public static Token EOF { get; } = new Token("EOF");

        public string Value { get; }

        public Token(string value) {
            this.Value = value;
        }

        public override string ToString() {
            return this.Value;
        }
    }

    public class IntToken : Token {
        public new int Value { get; }

        public IntToken(int value) : base(value.ToString()) {
            this.Value = value;
        }
    }

    public class BoolToken : Token {
        public new bool Value { get; }

        public BoolToken(bool value) : base(value.ToString()) {
            this.Value = value;
        }
    }

    public class IdentifierToken : Token {
        public IdentifierToken(string value) : base(value) {
        }
    }
}