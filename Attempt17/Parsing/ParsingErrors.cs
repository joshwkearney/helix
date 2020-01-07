using System;

namespace Attempt17.Parsing {
    public static class ParsingErrors {
        public static Exception EndOfFile(TokenLocation location) {
            return new CompilerException(
                location,
                "Unexpected End of File",
                "A token was expected, but the end of the file was reached");
        }

        public static Exception UnexpectedToken(TokenKind expected, IToken actual) {
            return new CompilerException(
                actual.Location,
                "Unexpected Token",
                $"Expected token '{expected}', recieved '{actual.Kind}'");
        }

        public static Exception UnexpectedToken(IToken actual) {
            return new CompilerException(
                actual.Location,
                "Unexpected Token",
                $"Unexpected token '{actual.Kind}'");
        }

        public static Exception UnexpectedCharacter(TokenLocation loc, char c) {
            return new CompilerException(
                loc,
                "Unexpected Character",
                $"Unexpected character '{c}'");
        }

        public static Exception InvalidNumber(TokenLocation loc, string num) {
            return new CompilerException(
                loc,
                "Invalid Numeric Literal",
                $"Unsuccessfully attempted to parse invalid numeric literal '{num}'");
        }
    }
}