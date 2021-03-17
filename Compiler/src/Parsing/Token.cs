namespace Trophy.Parsing {
    public enum TokenKind {
        OpenParenthesis, CloseParenthesis,
        OpenBrace, CloseBrace,
        OpenBracket, CloseBracket,

        AssignmentSign, LeftArrow, RightArrow, LiteralSign, YieldSign,
        NotSign, EqualSign, NotEqualSign, GreaterThanSign, LessThanSign,
        LessThanOrEqualToSign, GreaterThanOrEqualToSign,
        Comma, Colon, Dot, Semicolon, Pipe,

        VarKeyword, RefKeyword, IntKeyword, VoidKeyword, BoolKeyword, ArrayKeyword,
        IfKeyword, ThenKeyword, ElseKeyword, WhileKeyword, ForKeyword, DoKeyword, ToKeyword,
        FunctionKeyword, NewKeyword, StructKeyword, ClassKeyword, UnionKeyword,
        AsKeyword, IsKeyword, AllocKeyword, FromKeyword,
        TrueKeyword, FalseKeyword,
        AndKeyword, OrKeyword, XorKeyword,
        StackKeyword, HeapKeyword, RegionKeyword,

        Identifier, Whitespace,
        IntLiteral, BoolLiteral,

        MultiplySign, AddSign, SubtractSign, ModuloSign
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