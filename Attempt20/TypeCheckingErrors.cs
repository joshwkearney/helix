using System;
using System.Collections.Generic;
using System.Linq;

namespace Attempt20 {
    public class CompilerException : Exception {
        public string Title { get; }

        public TokenLocation Location { get; }

        public CompilerException(TokenLocation location, string title, string message) : base(message) {
            this.Location = location;
            this.Title = title;
        }
    }

    public static class TypeCheckingErrors {
        public static Exception UnexpectedType(TokenLocation location, LanguageType expected, LanguageType actual) {
            return new CompilerException(
                location,
                "Unexpected type",
                $"Expected type '{expected}', recieved type '{actual}'");
        }

        public static Exception UnexpectedType(TokenLocation location, LanguageType actual) {
            return new CompilerException(
                location,
                "Unexpected type",
                $"Unexpected type '{actual}'");
        }

        public static Exception VariableScopeExceeded(TokenLocation location, string variableName) {
            return new CompilerException(
                location,
                "Variable Scope Exceeded",
                $"The variable '{variableName}' was improperly passed beyond its applicable scope");
        }

        public static Exception LifetimeExceeded(TokenLocation location, IdentifierPath expected, IdentifierPath actual) {
            return new CompilerException(
                location,
                "Variable Lifetime Exceeded in Store",
                $"The lifetime '{actual}' does not outlive the lifetime '{expected}'");
        }

        public static Exception VariableUndefined(TokenLocation location, string name) {
            return new CompilerException(
                location,
                "Variable Undefined",
                $"The variable '{name}' is not defined in the current scope");
        }

        public static Exception RegionUndefined(TokenLocation location, string name) {
            return new CompilerException(
                location,
                "Region Undefined",
                $"The region '{name}' is not defined in the current scope");
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

        public static Exception ExpectedStructType(TokenLocation location, LanguageType actual) {
            return new CompilerException(
                location,
                "Expected Struct Type",
                $"Expected a struct type, but recieved '{actual}'");
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

        public static Exception ZeroLengthArrayLiteral(TokenLocation loc) {
            return new CompilerException(
                loc,
                "Invalid Array Literal",
                $"Array literals cannot be of zero length. Try using 'new T[0]' instead.");
        }

        public static Exception MemberUndefined(TokenLocation loc, LanguageType containingType, string memberName) {
            return new CompilerException(
                loc,
                "Member Name Undefined",
                $"The member '{memberName}' is undefined on the type '{containingType}'");
        }

        public static Exception AccessedFunctionParameterLikeVariable(TokenLocation loc, string par) {
            return new CompilerException(
                loc,
                "Invalid Variable Access",
                $"The function parameter '{par}' cannot be accessed as if it were a variable type.");
        }

        public static Exception NewObjectMissingFields(TokenLocation loc, LanguageType typeName, IEnumerable<string> missingFields) {
            string missing = string.Join(", ", missingFields.Select(x => "'" + x + "'"));

            return new CompilerException(
                loc,
                "Fields Missing in Object Instantiation",
                $"The following fields are missing in the instantiation of an object of type '{typeName.ToString()}': {missing}");
        }

        public static Exception NewObjectHasExtraneousFields(TokenLocation loc, LanguageType typeName, IEnumerable<string> extraFields) {
            string extra = string.Join(", ", extraFields.Select(x => "'" + x + "'"));

            return new CompilerException(
                loc,
                "Extraneous Fields Present in Object Instantiation",
                $"The following extraneous fields are present in the instantiation of an object of type '{typeName.ToString()}': {extra}");
        }

        public static Exception CircularValueObject(TokenLocation loc, LanguageType typeName) {
            return new CompilerException(
                loc,
                "Invalid Circular Object",
                $"The type '{typeName}' is directly circular, which is invalid in this context. Try using a layer of indirection for circular objects.");
        }

        public static Exception MethodNotDefinedOnUnionMember(TokenLocation loc, string union, string methodName, string memberName) {
            return new CompilerException(
                loc,
                "Method not defined on union member",
                $"The method '{methodName}' must be defined on the union member '{memberName}' in '{union}'");
        }

        public static Exception IncorrectUnionMethodSignature(TokenLocation loc, string union, string methodName, string memberName) {
            return new CompilerException(
                loc,
                "Incorrect Signature on union member",
                $"The method '{methodName}' on union member '{memberName}' does not match the signature defined on the union type '{union}'");
        }
    }
}