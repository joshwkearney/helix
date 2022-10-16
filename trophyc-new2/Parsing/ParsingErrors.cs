namespace Trophy.Parsing {
    public static class ParsingErrors {
        public static Exception EndOfFile(TokenLocation location) {
            return new ParseException(
                location,
                "Parse Exception: Unexpected End of File",
                "A token was expected, but the end of the file was reached");
        }

        public static Exception UnexpectedToken(TokenKind expected, Token actual) {
            return new ParseException(
                actual.Location,
                "Parse Exception: Unexpected Token",
                $"Expected token '{expected}', received '{actual.Kind}'");
        }

        public static Exception UnexpectedToken(Token actual) {
            return new ParseException(
                actual.Location,
                "Parse Exception: Unexpected Token",
                $"Unexpected token '{actual.Kind}'");
        }

        public static Exception UnexpectedCharacter(TokenLocation loc, char c) {
            return new ParseException(
                loc,
                "Parse Exception: Unexpected Character",
                $"Unexpected character '{c}'");
        }

        public static Exception UnexpectedSequence(TokenLocation loc) {
            return new ParseException(
                loc,
                "Parse Exception: Unexpected Sequence",
                $"Unexpected sequence of characters");
        }

        public static Exception InvalidNumber(TokenLocation loc, string num) {
            return new ParseException(
                loc,
                "Parse Exception: Invalid Numeric Literal",
                $"Unsuccessfully attempted to parse invalid numeric literal '{num}'");
        }
    }
}