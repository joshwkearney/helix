namespace JoshuaKearney.Attempt15.Parsing {
    public enum TokenKind {
        Int, Real, Boolean,

        AdditionSign, SubtractionSign,
        MultiplicationSign, DivisionSign, StrictDivisionSign,
        ExponentSign,
        
        NotKeyword,
        AndKeyword,
        XorKeyword,
        OrKeyword,

        SetKeyword, ToKeyword,

        OpenParenthesis, CloseParenthesis,
        OpenBrace, CloseBrace,

        LetKeyword, VarKeyword,
        FunctionKeyword, Arrow, EvokeKeyword,        
        IfKeyword, ThenKeyword, ElseKeyword,
        TypeKeyword,

        EqualsSign,
        GreaterThanSign,
        LessThanSign,
        EqualToSign,
        SpaceshipSign,

        Identifier,
        Comma,
        Semicolon,
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