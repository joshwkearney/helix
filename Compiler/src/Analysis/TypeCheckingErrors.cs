using Trophy.Analysis.Types;
using Trophy.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trophy.Analysis {
    public static class TypeCheckingErrors {
        public static Exception EarlyReturnInLambda(TokenLocation location) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Invalid early return statement",
                $"Early return statements are not permitted inside of lambda expressions");
        }

        public static Exception IncompleteMatch(TokenLocation location) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Incomplete match expression",
                $"This match expression does not cover all possible values of the target expression");
        }

        public static Exception ExpectedTypeExpression(TokenLocation location) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Expected a type expression",
                $"Expected a type expression but recieved a value expression");
        }

        public static Exception UnexpectedType(TokenLocation location, ITrophyType expected, ITrophyType actual) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Unexpected type",
                $"Expected type '{expected}', recieved type '{actual}'");
        }

        public static Exception UnexpectedType(TokenLocation location, ITrophyType actual) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Unexpected type",
                $"Unexpected type '{actual}'");
        }

        public static Exception VariableScopeExceeded(TokenLocation location, string variableName) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Variable Scope Exceeded",
                $"The variable '{variableName}' was improperly passed beyond its applicable scope");
        }

        public static Exception LifetimeExceeded(TokenLocation location, IdentifierPath expected, IdentifierPath actual) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Variable Lifetime Exceeded in Store",
                $"The lifetime '{actual}' does not outlive the lifetime '{expected}'");
        }

        public static Exception VariableUndefined(TokenLocation location, string name) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Variable Undefined",
                $"The variable '{name}' is not defined in the current scope");
        }

        public static Exception RegionUndefined(TokenLocation location, string name) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Region Undefined",
                $"The region '{name}' is not defined in the current scope");
        }

        public static Exception IdentifierDefined(TokenLocation location, string name) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Identifier Already Defined",
                $"The identifier '{name}' is already defined in the current scope");
        }

        public static Exception ExpectedVariableType(TokenLocation location, ITrophyType actual) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Expected Variable Type",
                $"Expected a variable type, but recieved '{actual}'");
        }

        public static Exception ExpectedFunctionType(TokenLocation location, ITrophyType actual) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Expected Function Type",
                $"Expected a function type, but recieved '{actual}'");
        }

        public static Exception ExpectedArrayType(TokenLocation location, ITrophyType actual) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Expected Array Type",
                $"Expected an array type, but recieved '{actual}'");
        }

        public static Exception ExpectedStructType(TokenLocation location, ITrophyType actual) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Expected Struct Type",
                $"Expected a struct type, but recieved '{actual}'");
        }

        public static Exception ExpectedUnionType(TokenLocation location, ITrophyType actual) {
            return new TypeCheckingException(
                location,
                "Analysis Exception: Expected Union Type",
                $"Expected a union type, but recieved '{actual}'");
        }

        public static Exception TypeUndefined(TokenLocation loc, string name) {
            return new TypeCheckingException(
                loc,
                "Analysis Exception: Undefined Type",
                $"The type '{name}' cannot be resolved in this context");
        }

        public static Exception ParameterCountMismatch(TokenLocation loc, int expectedCount, int actualCount) {
            return new TypeCheckingException(
                loc,
                "Analysis Exception: Parameter Count Mismatch",
                $"Expected {expectedCount} parameters, but recieved {actualCount}");
        }

        public static Exception TypeNotCopiable(TokenLocation loc, ITrophyType type) {
            return new TypeCheckingException(
                loc,
                "Analysis Exception: Type is Not Copiable",
                $"A copy cannot be made of type '{type}' because the type is not copiable. Try implementing a .copy() method.");
        }

        public static Exception TypeWithoutDefaultValue(TokenLocation loc, ITrophyType type) {
            return new TypeCheckingException(
                loc,
                "Analysis Exception: Type Does Not Have Default Value",
                $"The type '{type}' does not have a default value, and cannot be used in this context.");
        }

        public static Exception ArraySizeMustBeNonnegative(TokenLocation loc, long count) {
            return new TypeCheckingException(
                loc,
                "Analysis Exception: Array Size Must be Nonnegative",
                $"The size '{count}' is invalid for an array initializer; arrays must have nonnegative size.");
        }

        public static Exception ZeroLengthArrayLiteral(TokenLocation loc) {
            return new TypeCheckingException(
                loc,
                "Analysis Exception: Invalid Array Literal",
                $"Array literals cannot be of zero length. Try using 'new T[0]' instead.");
        }

        public static Exception MemberUndefined(TokenLocation loc, ITrophyType containingType, string memberName) {
            return new TypeCheckingException(
                loc,
                "Analysis Exception: Member Name Undefined",
                $"The member '{memberName}' is undefined on the type '{containingType}'");
        }

        public static Exception AccessedFunctionParameterLikeVariable(TokenLocation loc, string par) {
            return new TypeCheckingException(
                loc,
                "Analysis Exception: Invalid Variable Access",
                $"The function parameter '{par}' cannot be accessed as if it were a variable type.");
        }

        public static Exception NewObjectMissingFields(TokenLocation loc, ITrophyType typeName, IEnumerable<string> missingFields) {
            string missing = string.Join(", ", missingFields.Select(x => "'" + x + "'"));

            return new TypeCheckingException(
                loc,
                "Analysis Exception: Fields Missing in Object Instantiation",
                $"The following fields are missing in the instantiation of an object of type '{typeName}': {missing}");
        }

        public static Exception NewObjectHasExtraneousFields(TokenLocation loc, ITrophyType typeName, IEnumerable<string> extraFields) {
            string extra = string.Join(", ", extraFields.Select(x => "'" + x + "'"));

            return new TypeCheckingException(
                loc,
                "Analysis Exception: Extraneous Fields Present in Object Instantiation",
                $"The following extraneous fields are present in the instantiation of an object of type '{typeName}': {extra}");
        }

        public static Exception CircularValueObject(TokenLocation loc, ITrophyType typeName) {
            return new TypeCheckingException(
                loc,
                "Analysis Exception: Invalid Circular Object",
                $"The type '{typeName}' is directly circular, which is invalid in this context. Try using a layer of indirection for circular objects.");
        }

        public static Exception MethodNotDefinedOnUnionMember(TokenLocation loc, string union, string methodName, string memberName) {
            return new TypeCheckingException(
                loc,
                "Analysis Exception: Method not defined on union member",
                $"The method '{methodName}' must be defined on the union member '{memberName}' in '{union}'");
        }

        public static Exception IncorrectUnionMethodSignature(TokenLocation loc, string union, string methodName, string memberName) {
            return new TypeCheckingException(
                loc,
                "Analysis Exception: Incorrect Signature on union member",
                $"The method '{methodName}' on union member '{memberName}' does not match the signature defined on the union type '{union}'");
        }
    }
}