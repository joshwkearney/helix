using System;

namespace Trophy.Parsing {
    public class ParseException : Exception {
        public string Title { get; }

        public TokenLocation Location { get; }

        public ParseException(TokenLocation location, string title, string message) : base(message) {
            this.Title = title;
            this.Location = location;
        }

        public static ParseException EndOfFile(TokenLocation location) {
            return new ParseException(
                location,
                "Parse Exception: Unexpected End of File",
                "A token was expected, but the end of the file was reached");
        }

        public static ParseException UnexpectedToken(TokenKind expected, Token actual) {
            return new ParseException(
                actual.Location,
                "Parse Exception: Unexpected Token",
                $"Expected token '{expected}', received '{actual.Kind}'");
        }

        public static ParseException UnexpectedToken(Token actual) {
            return new ParseException(
                actual.Location,
                "Parse Exception: Unexpected Token",
                $"Unexpected token '{actual.Kind}'");
        }

        public static ParseException UnexpectedCharacter(TokenLocation loc, char c) {
            return new ParseException(
                loc,
                "Parse Exception: Unexpected Character",
                $"Unexpected character '{c}'");
        }

        public static ParseException UnexpectedSequence(TokenLocation loc) {
            return new ParseException(
                loc,
                "Parse Exception: Unexpected Sequence",
                $"Unexpected sequence of characters");
        }

        public static ParseException InvalidNumber(TokenLocation loc, string num) {
            return new ParseException(
                loc,
                "Parse Exception: Invalid Numeric Literal",
                $"Unsuccessfully attempted to parse invalid numeric literal '{num}'");
        }
    }
}