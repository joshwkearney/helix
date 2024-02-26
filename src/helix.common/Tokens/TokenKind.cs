namespace Helix.Parsing {
    public enum TokenKind {
        OpenParenthesis, CloseParenthesis,
        OpenBrace, CloseBrace,
        OpenBracket, CloseBracket,

        Comma, Colon, Dot, Semicolon,
        Star, Plus, Minus, Modulo, Divide, Caret, Ampersand,

        Not, Equals, NotEquals, 
        LessThan, GreaterThan, LessThanOrEqualTo, GreaterThanOrEqualTo,

        VarKeyword, Assignment, 
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
}