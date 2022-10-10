namespace Trophy.Parsing {
    public enum TokenKind {
        OpenParenthesis, CloseParenthesis,
        OpenBrace, CloseBrace,
        OpenBracket, CloseBracket,

        SingleEqualsSign, LeftArrow, RightArrow, AtSign, DoubleRightArrow,
        ExclamationSign, DoubleEqualSign, NotEqualSign, GreaterThanSign, LessThanSign,
        LessThanOrEqualToSign, GreaterThanOrEqualToSign,
        Comma, Colon, Dot, Semicolon,

        LocKeyword, VarKeyword, RefKeyword, IntKeyword, VoidKeyword, BoolKeyword, ArrayKeyword, SpanKeyword,
        IfKeyword, ThenKeyword, ElseKeyword, WhileKeyword, ForKeyword, DoKeyword, ToKeyword,
        FunctionKeyword, NewKeyword, PutKeyword, StructKeyword, UnionKeyword,
        AsKeyword, IsKeyword, FromKeyword, MatchKeyword, NotKeyword,
        TrueKeyword, FalseKeyword,
        AndKeyword, OrKeyword, XorKeyword,
        StackKeyword, HeapKeyword, RegionKeyword, ReturnKeyword, AsyncKeyword,

        Identifier, Whitespace,
        IntLiteral, BoolLiteral,

        Asterisk, PlusSign, MinusSign, PercentSign, SlashSign, EOF
    }

    public struct Token {
        public TokenLocation Location { get; }

        public TokenKind Kind { get; }

        public object Payload { get; }

        public Token(TokenKind kind, TokenLocation location) : this(kind, location, null) { }

        public Token(TokenKind kind, TokenLocation location, object payload) {
            this.Kind = kind;
            this.Location = location;
            this.Payload = payload;
        }
    }
}