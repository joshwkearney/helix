using Attempt16.Parsing;
using System;

namespace Attempt16 {
    public static class CompilerErrors {
        public static void UnexpectedCharacter(char c) {
            throw new Exception($"Lexical Error : Invalid character { c }");
        }

        public static void InvalidIntegerFormat(string num) {
            throw new Exception($"Lexical Error : Invalid integer { num }");
        }

        public static void UnexpectedToken(IToken tok) {
            throw new Exception($"Parse Error : Unexpected token { tok }");
        }

        public static void UnexpectedToken(IToken tok, IToken expected) {
            throw new Exception($"Parse Error : Unexpected token { tok }, expected { expected }");
        }

        public static void UnexpectedToken<TExpected>(IToken tok) {
            throw new Exception($"Parse Error : Unexpected token { tok }, expected { nameof(TExpected) }");
        }

        public static void ExpectedValueSymbol() {
            throw new Exception("Semantic Error : Expected a value symbol");
        }

        public static void ExpectedTypeSymbol() {
            throw new Exception("Semantic Error : Expected a type symbol");
        }

        public static void UndeclaredVariable(string name) {
            throw new Exception($"Semantic Error : Variable { name } is undeclared");
        }

        public static void MismatchedTypes() {
            throw new Exception($"Semantic Error : Symbol types must match");
        }
    }
}