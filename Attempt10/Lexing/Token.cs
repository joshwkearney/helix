using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12 {
    public enum TokenKind {
        Int32Token,
        Real32Token,
        BooleanLiteral,
        AdditionSign,
        SubtractionSign,
        MultiplicationSign,
        RealDivisionSign,
        StrictDivisionSign,
        NotSign,
        OpenParenthesis,
        CloseParenthesis,
        OpenBrace,
        CloseBrace,
        LetKeyword,
        VarKeyword,
        FunctionKeyword,
        AndKeyword,
        OrKeyword,
        XorKeyword,
        IfKeyword,
        ThenKeyword,
        ElseKeyword,
        BoxKeyword,
        UnboxKeyword,
        Arrow,
        Semicolon,
        EqualsSign,
        Identifier,
        Comma,
        PipeRight,
        GreaterThanSign,
        LessThanSign,
        EvokeKeyword,
        Dot,
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