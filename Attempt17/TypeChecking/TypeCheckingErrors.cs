using Attempt17.Features.Primitives;
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

        public static Exception ExpectedArrayType(TokenLocation location, LanguageType actual) {
            return new CompilerException(
                location,
                "Expected Array Type",
                $"Expected an array type, but recieved '{actual}'");
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

        public static Exception TypeNotCopiable(TokenLocation loc, LanguageType type) {
            return new CompilerException(
                loc,
                "Type is Not Copiable",
                $"A copy cannot be made of type '{type}' because the type is not copiable. Try implementing a .copy() method.");
        }

        public static Exception StoredToCapturedVariable(TokenLocation loc, IdentifierPath captured, IdentifierPath capturing) {
            return new CompilerException(
                loc,
                "Invalid Store to Captured Variable",
                $"A variable store is invalid because the variable '{captured.Segments.Last()}' was value-captured by the variable '{capturing.Segments.Last()}'");
        }

        public static Exception AccessedMovedVariable(TokenLocation loc, IdentifierPath variable) {
            return new CompilerException(
                loc,
                "Accessed Moved Variable",
                $"Cannot access variable '{variable.Segments.Last()}', for it has been moved.");
        }

        public static Exception MovedCapturedVariable(TokenLocation loc, IdentifierPath captured, IdentifierPath capturing) {
            return new CompilerException(
                loc,
                "Invalid Move of Captured Variable",
                $"A variable move is invalid because the variable '{captured.Segments.Last()}' was captured by the variable '{capturing.Segments.Last()}'");
        }

        public static Exception MovedUnmovableVariable(TokenLocation loc, IdentifierPath variable) {
            return new CompilerException(
                loc,
                "Cannot Move Unmovable Variable",
                $"The variable '{variable.Segments.Last()}' cannot be moved because it is not movable. Try allocating it instead.");
        }

        public static Exception MovedInWhileLoop(TokenLocation loc, IdentifierPath variable) {
            return new CompilerException(
                loc,
                "Cannot Move Inside Loops",
                $"The variable '{variable.Segments.Last()}' cannot be moved inside of a while loop.");
        }

        public static Exception TypeWithoutDefaultValue(TokenLocation loc, LanguageType type) {
            return new CompilerException(
                loc,
                "Type Does Not Have Default Value",
                $"The type '{type.ToString()}' does not have a default value, and cannot be used in this context.");
        }

        public static Exception ArraySizeMustBeNonnegative(TokenLocation loc, long count) {
            return new CompilerException(
                loc,
                "Array Size Must be Nonnegative",
                $"The size '{count}' is invalid for an array initializer; arrays must have nonnegative size.");
        }

        public static Exception InvalidBinaryOperator(TokenLocation loc, BinarySyntaxKind op, LanguageType type) {
            return new CompilerException(
                loc,
                "Invalid Binary Operator",
                $"The binary operator '{op}' is invalid for arguments of type '{type}'");
        }

        public static Exception ZeroLengthArrayLiteral(TokenLocation loc) {
            return new CompilerException(
                loc,
                "Invalid Array Literal",
                $"Array literals cannot be of zero length. Try using 'new T[0]' instead.");
        }

        public static Exception MemberUndefined(TokenLocation loc, LanguageType type, string memberName) {
            return new CompilerException(
                loc,
                "Member Name Undefined",
                $"The member '{memberName}' is undefined on the type '{type}'");
        }
    }
}