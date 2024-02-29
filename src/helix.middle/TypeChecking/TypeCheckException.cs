using Helix.Common;
using Helix.Common.Tokens;
using Helix.Common.Types;

namespace Helix.MiddleEnd.TypeChecking {
    public class TypeCheckException : HelixException {
        public TypeCheckException(TokenLocation location, string title, string message) : base(location, title, message) { }

        public static TypeCheckException NewUnionMultipleMembers(TokenLocation location) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Invalid New Union",
                $"New unions initializers can have at most one field assignment.");
        }

        public static TypeCheckException TypeConversionFailed(TokenLocation location, IHelixType fromType, IHelixType toType) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Failed type conversion",
                $"Unable to convert type '{fromType}' to '{toType}'");
        }

        public static TypeCheckException TypeUnificationFailed(TokenLocation location, IHelixType type1, IHelixType type2) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Failed type unification",
                $"Unable to unify the types '{type1}' to '{type2}' into a common type");
        }

        public static TypeCheckException TypeWithoutVoidValue(TokenLocation loc, IHelixType type) {
            return new TypeCheckException(
                loc,
                "Analysis Exception: Type does not have a void value",
                $"The type '{type}' does not have a void value and cannot be used in this context.");
        }

        public static TypeCheckException MemberUndefined(TokenLocation loc, IHelixType containingType, string memberName) {
            return new TypeCheckException(
                loc,
                "Analysis Exception: Member Name Undefined",
                $"The member '{memberName}' is undefined on the type '{containingType}'");
        }

        public static TypeCheckException CircularValueObject(TokenLocation loc, IHelixType typeName) {
            return new TypeCheckException(
                loc,
                "Analysis Exception: Invalid circular object",
                $"The type '{typeName}' is directly circular, which is invalid in this context. Try using a layer of indirection for circular objects.");
        }

        public static TypeCheckException InvalidBinaryOperator(TokenLocation loc, IHelixType type1, IHelixType type2, BinaryOperationKind op) {
            return new TypeCheckException(
                loc,
                "Analysis Exception: Invalid binary operator",
                $"The operator '{op}' is not valid on the types '{type1}' and '{type2}'");
        }

        public static TypeCheckException NotInLoop(TokenLocation loc) {
            return new TypeCheckException(
                loc,
                "Analysis Exception: No loop",
                $"This statement is only valid inside of a loop");
        }

        // ### OLD

        public static TypeCheckException NoReturn(TokenLocation location) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Invalid function body",
                $"This function does not return a value on all code paths.");
        }

        public static TypeCheckException RValueRequired(TokenLocation location) {
            return new TypeCheckException(
                location,
                "Analysis Exception: RValue Required",
                $"An rvalue is required in this context. Are you trying to store a type in a variable?");
        }

        public static TypeCheckException LValueRequired(TokenLocation location) {
            return new TypeCheckException(
                location,
                "Analysis Exception: LValue Required",
                $"An lvalue is required in this context. Are you trying to assign to a read-only variable?");
        }

        public static TypeCheckException ExpectedTypeExpression(TokenLocation location) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Expected a type expression",
                $"Expected a type expression but recieved a value expression");
        }

        public static TypeCheckException UnexpectedType(TokenLocation location, IHelixType expected, IHelixType actual) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Unexpected type",
                $"Expected type '{expected}', recieved type '{actual}'");
        }

        public static TypeCheckException UnexpectedType(TokenLocation location, IHelixType actual) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Unexpected type",
                $"Unexpected type '{actual}'");
        }

        public static TypeCheckException VariableUndefined(TokenLocation location, string name) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Variable Undefined",
                $"The variable '{name}' is not defined in the current scope");
        }

        public static TypeCheckException RegionUndefined(TokenLocation location, string name) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Region Undefined",
                $"The region '{name}' is not defined in the current scope");
        }

        public static TypeCheckException IdentifierDefined(TokenLocation location, string name) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Identifier Already Defined",
                $"The identifier '{name}' is already defined in the current scope");
        }

        public static TypeCheckException ExpectedVariableType(TokenLocation location, IHelixType actual) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Expected Variable Type",
                $"Expected a variable type, but recieved '{actual}'");
        }

        public static TypeCheckException ExpectedFunctionType(TokenLocation location, IHelixType actual) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Expected Function Type",
                $"Expected a function type, but recieved '{actual}'");
        }

        public static TypeCheckException ExpectedArrayType(TokenLocation location, IHelixType actual) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Expected Array Type",
                $"Expected an array type, but recieved '{actual}'");
        }

        public static TypeCheckException ExpectedStructType(TokenLocation location, IHelixType actual) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Expected Struct Type",
                $"Expected a struct type, but recieved '{actual}'");
        }

        public static TypeCheckException ExpectedUnionType(TokenLocation location) {
            return new TypeCheckException(
                location,
                "Analysis Exception: Expected Union Type",
                $"A union type is required in this context.");
        }

        public static TypeCheckException TypeUndefined(TokenLocation loc, string name) {
            return new TypeCheckException(
                loc,
                "Analysis Exception: Undefined Type",
                $"The type '{name}' cannot be resolved in this context");
        }

        public static TypeCheckException ParameterCountMismatch(TokenLocation loc, int expectedCount, int actualCount) {
            return new TypeCheckException(
                loc,
                "Analysis Exception: Parameter Count Mismatch",
                $"Expected {expectedCount} parameters, but recieved {actualCount}");
        }

        public static TypeCheckException ArraySizeMustBeNonnegative(TokenLocation loc, long count) {
            return new TypeCheckException(
                loc,
                "Analysis Exception: Array Size Must be Nonnegative",
                $"The size '{count}' is invalid for an array initializer; arrays must have nonnegative size.");
        }

        public static TypeCheckException ZeroLengthArrayLiteral(TokenLocation loc) {
            return new TypeCheckException(
                loc,
                "Analysis Exception: Invalid Array Literal",
                $"Array literals cannot be of zero length. Try using 'new T[0]' instead.");
        }

        public static TypeCheckException AccessedFunctionParameterLikeVariable(TokenLocation loc, string par) {
            return new TypeCheckException(
                loc,
                "Analysis Exception: Invalid Variable Access",
                $"The function parameter '{par}' cannot be accessed as if it were a variable type.");
        }

        public static TypeCheckException NewObjectMissingFields(TokenLocation loc, IHelixType typeName, IEnumerable<string> missingFields) {
            string missing = string.Join(", ", missingFields.Select(x => "'" + x + "'"));

            return new TypeCheckException(
                loc,
                "Analysis Exception: Fields Missing in Object Instantiation",
                $"The following fields are missing in the instantiation of an object of type '{typeName}': {missing}");
        }

        public static TypeCheckException NewObjectHasExtraneousFields(TokenLocation loc, IHelixType typeName, IEnumerable<string> extraFields) {
            string extra = string.Join(", ", extraFields.Select(x => "'" + x + "'"));

            return new TypeCheckException(
                loc,
                "Analysis Exception: Extraneous Fields Present in Object Instantiation",
                $"The following extraneous fields are present in the instantiation of an object of type '{typeName}': {extra}");
        }

        public static TypeCheckException NewObjectHasExtraneousFields(TokenLocation loc, IHelixType typeName) {
            return new TypeCheckException(
                loc,
                "Analysis Exception: Extraneous Fields Present in Object Instantiation",
                $"The following extraneous fields are present in the instantiation of an object of type '{typeName}'");
        }        
    }
}