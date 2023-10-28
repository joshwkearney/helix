using Helix.Analysis.Types;
using Helix.Parsing;

namespace Helix.Analysis.TypeChecking {
    public class TypeException : HelixException {
        public TypeException(TokenLocation location, string title, string message) : base(location, title, message) { }

        public static TypeException WritingToConstVariable(TokenLocation location) {
            return new TypeException(
                location,
                "Analysis Exception: Read-only Variable",
                $"Cannot write to a read-only variable.");
        }

        public static TypeException WritingToConstPointer(TokenLocation location) {
            return new TypeException(
                location,
                "Analysis Exception: Read-only Pointer",
                $"Cannot write to a read-only pointer.");
        }

        public static TypeException RValueRequired(TokenLocation location) {
            return new TypeException(
                location,
                "Analysis Exception: RValue Required",
                $"An rvalue is required in this context. Are you trying to store a type in a variable?");
        }

        public static TypeException LValueRequired(TokenLocation location) {
            return new TypeException(
                location,
                "Analysis Exception: LValue Required",
                $"An lvalue is required in this context. Are you trying to assign to a read-only variable?");
        }

        public static TypeException InvalidHeapAllocation(TokenLocation location, HelixType type) {
            return new TypeException(
                location,
                "Analysis Exception: Invalid Heap Allocation",
                $"The type '{type}' cannot be allocated on the heap");
        }

        public static TypeException InvalidStackAllocation(TokenLocation location, HelixType type) {
            return new TypeException(
                location,
                "Analysis Exception: Invalid Stack Allocation",
                $"The type '{type}' cannot be allocated on the stack");
        }

        public static TypeException EarlyReturnInLambda(TokenLocation location) {
            return new TypeException(
                location,
                "Analysis Exception: Invalid early return statement",
                $"Early return statements are not permitted inside of lambda expressions");
        }

        public static TypeException IncompleteMatch(TokenLocation location) {
            return new TypeException(
                location,
                "Analysis Exception: Incomplete match expression",
                $"This match expression does not cover all possible values of the target expression");
        }

        public static TypeException ExpectedTypeExpression(TokenLocation location) {
            return new TypeException(
                location,
                "Analysis Exception: Expected a type expression",
                $"Expected a type expression but recieved a value expression");
        }

        public static TypeException UnexpectedType(TokenLocation location, HelixType expected, HelixType actual) {
            return new TypeException(
                location,
                "Analysis Exception: Unexpected type",
                $"Expected type '{expected}', recieved type '{actual}'");
        }

        public static TypeException UnexpectedType(TokenLocation location, HelixType actual) {
            return new TypeException(
                location,
                "Analysis Exception: Unexpected type",
                $"Unexpected type '{actual}'");
        }

        public static TypeException VariableScopeExceeded(TokenLocation location, string variableName) {
            return new TypeException(
                location,
                "Analysis Exception: Variable Scope Exceeded",
                $"The variable '{variableName}' was improperly passed beyond its applicable scope");
        }

        public static TypeException LifetimeExceeded(TokenLocation location, IdentifierPath expected, IdentifierPath actual) {
            return new TypeException(
                location,
                "Analysis Exception: Variable Lifetime Exceeded in Store",
                $"The lifetime '{actual}' does not outlive the lifetime '{expected}'");
        }

        public static TypeException VariableUndefined(TokenLocation location, string name) {
            return new TypeException(
                location,
                "Analysis Exception: Variable Undefined",
                $"The variable '{name}' is not defined in the current scope");
        }

        public static TypeException RegionUndefined(TokenLocation location, string name) {
            return new TypeException(
                location,
                "Analysis Exception: Region Undefined",
                $"The region '{name}' is not defined in the current scope");
        }

        public static TypeException IdentifierDefined(TokenLocation location, string name) {
            return new TypeException(
                location,
                "Analysis Exception: Identifier Already Defined",
                $"The identifier '{name}' is already defined in the current scope");
        }

        public static TypeException ExpectedVariableType(TokenLocation location, HelixType actual) {
            return new TypeException(
                location,
                "Analysis Exception: Expected Variable Type",
                $"Expected a variable type, but recieved '{actual}'");
        }

        public static TypeException ExpectedFunctionType(TokenLocation location, HelixType actual) {
            return new TypeException(
                location,
                "Analysis Exception: Expected Function Type",
                $"Expected a function type, but recieved '{actual}'");
        }

        public static TypeException ExpectedArrayType(TokenLocation location, HelixType actual) {
            return new TypeException(
                location,
                "Analysis Exception: Expected Array Type",
                $"Expected an array type, but recieved '{actual}'");
        }

        public static TypeException ExpectedStructType(TokenLocation location, HelixType actual) {
            return new TypeException(
                location,
                "Analysis Exception: Expected Struct Type",
                $"Expected a struct type, but recieved '{actual}'");
        }

        public static TypeException ExpectedUnionType(TokenLocation location) {
            return new TypeException(
                location,
                "Analysis Exception: Expected Union Type",
                $"A union type is required in this context.");
        }

        public static TypeException TypeUndefined(TokenLocation loc, string name) {
            return new TypeException(
                loc,
                "Analysis Exception: Undefined Type",
                $"The type '{name}' cannot be resolved in this context");
        }

        public static TypeException ParameterCountMismatch(TokenLocation loc, int expectedCount, int actualCount) {
            return new TypeException(
                loc,
                "Analysis Exception: Parameter Count Mismatch",
                $"Expected {expectedCount} parameters, but recieved {actualCount}");
        }

        public static TypeException TypeNotCopiable(TokenLocation loc, HelixType type) {
            return new TypeException(
                loc,
                "Analysis Exception: Type is Not Copiable",
                $"A copy cannot be made of type '{type}' because the type is not copiable. Try implementing a .copy() method.");
        }

        public static TypeException TypeWithoutDefaultValue(TokenLocation loc, HelixType type) {
            return new TypeException(
                loc,
                "Analysis Exception: Type Does Not Have Default Value",
                $"The type '{type}' does not have a default value, and cannot be used in this context.");
        }

        public static TypeException ArraySizeMustBeNonnegative(TokenLocation loc, long count) {
            return new TypeException(
                loc,
                "Analysis Exception: Array Size Must be Nonnegative",
                $"The size '{count}' is invalid for an array initializer; arrays must have nonnegative size.");
        }

        public static TypeException ZeroLengthArrayLiteral(TokenLocation loc) {
            return new TypeException(
                loc,
                "Analysis Exception: Invalid Array Literal",
                $"Array literals cannot be of zero length. Try using 'new T[0]' instead.");
        }

        public static TypeException MemberUndefined(TokenLocation loc, HelixType containingType, string memberName) {
            return new TypeException(
                loc,
                "Analysis Exception: Member Name Undefined",
                $"The member '{memberName}' is undefined on the type '{containingType}'");
        }

        public static TypeException AccessedFunctionParameterLikeVariable(TokenLocation loc, string par) {
            return new TypeException(
                loc,
                "Analysis Exception: Invalid Variable Access",
                $"The function parameter '{par}' cannot be accessed as if it were a variable type.");
        }

        public static TypeException NewObjectMissingFields(TokenLocation loc, HelixType typeName, IEnumerable<string> missingFields) {
            string missing = string.Join(", ", missingFields.Select(x => "'" + x + "'"));

            return new TypeException(
                loc,
                "Analysis Exception: Fields Missing in Object Instantiation",
                $"The following fields are missing in the instantiation of an object of type '{typeName}': {missing}");
        }

        public static TypeException NewObjectHasExtraneousFields(TokenLocation loc, HelixType typeName, IEnumerable<string> extraFields) {
            string extra = string.Join(", ", extraFields.Select(x => "'" + x + "'"));

            return new TypeException(
                loc,
                "Analysis Exception: Extraneous Fields Present in Object Instantiation",
                $"The following extraneous fields are present in the instantiation of an object of type '{typeName}': {extra}");
        }

        public static TypeException CircularValueObject(TokenLocation loc, HelixType typeName) {
            return new TypeException(
                loc,
                "Analysis Exception: Invalid Circular Object",
                $"The type '{typeName}' is directly circular, which is invalid in this context. Try using a layer of indirection for circular objects.");
        }

        public static TypeException MethodNotDefinedOnUnionMember(TokenLocation loc, string union, string methodName, string memberName) {
            return new TypeException(
                loc,
                "Analysis Exception: Method not defined on union member",
                $"The method '{methodName}' must be defined on the union member '{memberName}' in '{union}'");
        }

        public static TypeException IncorrectUnionMethodSignature(TokenLocation loc, string union, string methodName, string memberName) {
            return new TypeException(
                loc,
                "Analysis Exception: Incorrect Signature on union member",
                $"The method '{methodName}' on union member '{memberName}' does not match the signature defined on the union type '{union}'");
        }
    }
}