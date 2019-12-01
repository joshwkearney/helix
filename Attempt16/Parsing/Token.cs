namespace Attempt16.Parsing {
    public enum TokenKind {
        OpenParenthesis, CloseParenthesis,
        OpenBrace, CloseBrace,

        EqualSign, LeftArrow, LiteralSign, YieldSign,
        Comma, Colon, Dot,

        VarKeyword, RefKeyword, IntKeyword, VoidKeyword,
        AllocKeyword, FreeKeyword, CopyKeyword,
        IfKeyword, ThenKeyword, ElseKeyword, WhileKeyword, DoKeyword,
        FunctionKeyword, NewKeyword, StructKeyword,

        Identifier, Whitespace, IntLiteral,

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