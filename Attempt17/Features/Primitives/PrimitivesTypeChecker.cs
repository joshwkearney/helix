using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Primitives {
    public class PrimitivesTypeChecker {
        public ISyntax<TypeCheckTag> CheckIntLiteral(IntLiteralSyntax<ParseTag> literal, Scope scope, ITypeChecker checker) {
            var tag = new TypeCheckTag(IntType.Instance);

            return new IntLiteralSyntax<TypeCheckTag>(tag, literal.Value);
        }

        public ISyntax<TypeCheckTag> CheckVoidLiteral(VoidLiteralSyntax<ParseTag> literal, Scope scope, ITypeChecker checker) {
            var tag = new TypeCheckTag(VoidType.Instance);

            return new VoidLiteralSyntax<TypeCheckTag>(tag);
        }

        public ISyntax<TypeCheckTag> CheckBinarySyntax(BinarySyntax<ParseTag> syntax, Scope scope, ITypeChecker checker) {
            var left = checker.Check(syntax.Left, scope);
            var right = checker.Check(syntax.Right, scope);

            if (left.Tag.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(syntax.Left.Tag.Location, IntType.Instance, left.Tag.ReturnType);
            }

            if (right.Tag.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(syntax.Right.Tag.Location, IntType.Instance, right.Tag.ReturnType);
            }

            var tag = new TypeCheckTag(
                IntType.Instance,
                left.Tag.CapturedVariables.Union(right.Tag.CapturedVariables));

            return new BinarySyntax<TypeCheckTag>(tag, syntax.Kind, left, right);
        }
    }
}