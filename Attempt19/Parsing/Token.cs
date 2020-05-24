namespace Attempt19.Parsing {
    public enum TokenKind {
        OpenParenthesis, CloseParenthesis,
        OpenBrace, CloseBrace,
        OpenBracket, CloseBracket,

        AssignmentSign, LeftArrow, LiteralSign, YieldSign,
        NotSign, EqualSign, NotEqualSign, GreaterThanSign, LessThanSign,
        LessThanOrEqualToSign, GreaterThanOrEqualToSign,
        Comma, Colon, Dot, Semicolon,

        VarKeyword, RefKeyword, IntKeyword, VoidKeyword, BoolKeyword,
        MoveKeyword, AllocKeyword,
        IfKeyword, ThenKeyword, ElseKeyword, WhileKeyword, DoKeyword,
        FunctionKeyword, NewKeyword, StructKeyword, ClassKeyword, UnionKeyword,
        AsKeyword,
        TrueKeyword, FalseKeyword,
        AndKeyword, OrKeyword, XorKeyword,

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
}