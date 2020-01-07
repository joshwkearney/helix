using Attempt17.Parsing;
using Attempt17.Types;
using System;
using System.Linq;

namespace Attempt17.TypeChecking {
    public static class TypeCheckingErrors {
        public static Exception UnexpectedType(TokenLocation location, LanguageType expected, LanguageType actual) {
            return new CompilerException(
                location,
                "Unexpected type",
                $"Expected type '{expected}', recieved type '{actual}'");
        }

        public static Exception VariableScopeExceeded(TokenLocation location, IdentifierPath variable) {
            return new CompilerException(
                location,
                "Variable Scope Exceeded",
                $"The variable '{variable.Segments.Last()}' was improperly passed beyond its applicable scope");
        }

        public static Exception StoreScopeExceeded(TokenLocation location, IdentifierPath target, IdentifierPath variable) {
            return new CompilerException(
                location,
                "Variable Scope Exceeded in Store",
                $"The variable '{variable.Segments.Last()}' does not outlive '{target.Segments.Last()}', and was improperly passed beyond its applicable scope");
        }

        public static Exception VariableUndefined(TokenLocation location, string name) {
            return new CompilerException(
                location,
                "Variable Undefined",
                $"The variable '{name}' is not defined in the current scope");
        }

        public static Exception IdentifierDefined(TokenLocation location, string name) {
            return new CompilerException(
                location,
                "Identifier Already Defined",
                $"The identifier '{name}' is already defined in the current scope");
        }

        public static Exception ExpectedVariableType(TokenLocation location, LanguageType actual) {
            return new CompilerException(
                location,
                "Expected Variable Type",
                $"Expected a variable type, but recieved '{actual}'");
        }

        public static Exception ExpectedFunctionType(TokenLocation location, LanguageType actual) {
            return new CompilerException(
                location,
                "Expected Function Type",
                $"Expected a function type, but recieved '{actual}'");
        }

        public static Exception TypeUndefined(TokenLocation loc, string name) {
            return new CompilerException(
                loc,
                "Undefined Type",
                $"The type '{name}' cannot be resolved in this context");
        }

        public static Exception ParameterCountMismatch(TokenLocation loc, int expectedCount, int actualCount) {
            return new CompilerException(
                loc,
                "Parameter Count Mismatch",
                $"Expected {expectedCount} parameters, but recieved {actualCount}");
        }
    }
}