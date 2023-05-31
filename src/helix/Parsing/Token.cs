namespace Helix.Parsing {
    public enum TokenKind {
        OpenParenthesis, CloseParenthesis,
        OpenBrace, CloseBrace,
        OpenBracket, CloseBracket,

        Comma, Colon, Dot, Semicolon,
        Star, Plus, Minus, Modulo, Divide, Caret, Ampersand,

        Not, Equals, NotEquals, 
        LessThan, GreaterThan, LessThanOrEqualTo, GreaterThanOrEqualTo,

        VarKeyword, LetKeyword, Assignment, 
        PlusAssignment, MinusAssignment, StarAssignment, DivideAssignment, ModuloAssignment,
        FunctionKeyword, ExternKeyword, Yields,

        WordKeyword, VoidKeyword, BoolKeyword, AsKeyword, IsKeyword,

        IfKeyword, ThenKeyword, ElseKeyword, 
        WhileKeyword, ForKeyword, DoKeyword, ToKeyword, UntilKeyword,
        BreakKeyword, ContinueKeyword, ReturnKeyword,
        StructKeyword, UnionKeyword, NewKeyword,

        TrueKeyword, FalseKeyword, AndKeyword, OrKeyword, XorKeyword,

        Identifier, Whitespace, WordLiteral, BoolLiteral, EOF
    }

    public record struct Token {
        public TokenLocation Location { get; }

        public TokenKind Kind { get; }

        public string Value { get; }

        public Token(TokenKind kind, TokenLocation location, string payload) {
            this.Kind = kind;
            this.Location = location;
            this.Value = payload;
        }

        public override string ToString() {
            return $"Token(Value= {this.Value}, Location= {this.Location.StartIndex})";
        }
    }
}