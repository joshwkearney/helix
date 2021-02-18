using System;

namespace Attempt20 {
    public enum TokenKind {
        OpenParenthesis, CloseParenthesis,
        OpenBrace, CloseBrace,
        OpenBracket, CloseBracket,

        AssignmentSign, LeftArrow, LiteralSign, YieldSign,
        NotSign, EqualSign, NotEqualSign, GreaterThanSign, LessThanSign,
        LessThanOrEqualToSign, GreaterThanOrEqualToSign,
        Comma, Colon, Dot, Semicolon, Pipe,

        VarKeyword, RefKeyword, IntKeyword, VoidKeyword, BoolKeyword,
        IfKeyword, ThenKeyword, ElseKeyword, WhileKeyword, DoKeyword,
        FunctionKeyword, NewKeyword, StructKeyword, ClassKeyword, UnionKeyword,
        AsKeyword, AllocKeyword, FromKeyword,
        TrueKeyword, FalseKeyword,
        AndKeyword, OrKeyword, XorKeyword,
        StackKeyword, HeapKeyword, RegionKeyword,

        Identifier, Whitespace, 
        IntLiteral, BoolLiteral,

        MultiplySign, AddSign, SubtractSign
    }

    public interface IToken {
        TokenLocation Location { get; }

        TokenKind Kind { get; }
    }

    public class Token : IToken {
        public TokenLocation Location { get; }

        public TokenKind Kind { get; }

        public Token(TokenKind kind, TokenLocation location) {
            this.Kind = kind;
            this.Location = location;
        }
    }

    public class Token<T> : IToken {
        public T Value { get; }

        public TokenLocation Location { get; }

        public TokenKind Kind { get; }

        public Token(T value, TokenKind kind, TokenLocation location) {
            this.Value = value;
            this.Kind = kind;
            this.Location = location;
        }
    }

    public class TokenLocation {
        public int StartIndex { get; }

        public int Length { get; }

        public TokenLocation(int start, int length) {
            this.StartIndex = start;
            this.Length = length;
        }

        public TokenLocation Span(TokenLocation other) {
            if (other.StartIndex < this.StartIndex) {
                return other.Span(this);
            }
            else if (other.StartIndex == this.StartIndex) {
                return new TokenLocation(this.StartIndex, Math.Max(this.Length, other.Length));
            }
            else {
                return new TokenLocation(this.StartIndex, other.StartIndex - this.StartIndex + other.Length);
            }
        }
    }
}