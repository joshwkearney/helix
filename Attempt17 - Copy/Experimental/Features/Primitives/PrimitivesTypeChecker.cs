using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.Primitives {
    public class PrimitivesTypeChecker {
        public ISyntax<TypeCheckInfo> CheckIntLiteral(IntLiteralSyntax<ParseInfo> literal, Scope scope, ITypeChecker checker) {
            var tag = new TypeCheckInfo(IntType.Instance);

            return new IntLiteralSyntax<TypeCheckInfo>(tag, literal.Value);
        }

        public ISyntax<TypeCheckInfo> CheckVoidLiteral(VoidLiteralSyntax<ParseInfo> literal, Scope scope, ITypeChecker checker) {
            var tag = new TypeCheckInfo(VoidType.Instance);

            return new VoidLiteralSyntax<TypeCheckInfo>(tag);
        }

        public ISyntax<TypeCheckInfo> CheckBinarySyntax(BinarySyntax<ParseInfo> syntax, Scope scope, ITypeChecker checker) {
            var left = checker.Check(syntax.Left, scope);
            var right = checker.Check(syntax.Right, scope);

            if (left.Tag.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(syntax.Left.Tag.Location, IntType.Instance, left.Tag.ReturnType);
            }

            if (right.Tag.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(syntax.Right.Tag.Location, IntType.Instance, right.Tag.ReturnType);
            }

            var tag = new TypeCheckInfo(
                IntType.Instance,
                left.Tag.CapturedVariables.Union(right.Tag.CapturedVariables));

            return new BinarySyntax<TypeCheckInfo>(tag, syntax.Kind, left, right);
        }
    }
}