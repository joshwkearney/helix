using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System.Collections.Generic;

namespace Attempt17.Features.Primitives {
    public class PrimitivesTypeChecker {
        private readonly Dictionary<BinarySyntaxKind, LanguageType> intOperations = new Dictionary<BinarySyntaxKind, LanguageType>() {
            { BinarySyntaxKind.Add, IntType.Instance }, { BinarySyntaxKind.Subtract, IntType.Instance },
            { BinarySyntaxKind.Multiply, IntType.Instance }, { BinarySyntaxKind.And, IntType.Instance },
            { BinarySyntaxKind.Or, IntType.Instance }, { BinarySyntaxKind.Xor, IntType.Instance },
            { BinarySyntaxKind.EqualTo, BoolType.Instance }, { BinarySyntaxKind.NotEqualTo, BoolType.Instance },
            { BinarySyntaxKind.GreaterThan, BoolType.Instance }, { BinarySyntaxKind.LessThan, BoolType.Instance },
            { BinarySyntaxKind.GreaterThanOrEqualTo, BoolType.Instance }, { BinarySyntaxKind.LessThanOrEqualTo, BoolType.Instance },
        };

        private readonly Dictionary<BinarySyntaxKind, LanguageType> boolOperations = new Dictionary<BinarySyntaxKind, LanguageType>() {
            { BinarySyntaxKind.And, BoolType.Instance },
            { BinarySyntaxKind.Or, BoolType.Instance }, { BinarySyntaxKind.Xor, BoolType.Instance },            
            { BinarySyntaxKind.EqualTo, BoolType.Instance }, { BinarySyntaxKind.NotEqualTo, BoolType.Instance },
        };

        public ISyntax<TypeCheckTag> CheckIntLiteral(IntLiteralSyntax<ParseTag> literal, IScope scope, ITypeChecker checker) {
            var tag = new TypeCheckTag(IntType.Instance);

            return new IntLiteralSyntax<TypeCheckTag>(tag, literal.Value);
        }

        public ISyntax<TypeCheckTag> CheckVoidLiteral(VoidLiteralSyntax<ParseTag> literal, IScope scope, ITypeChecker checker) {
            var tag = new TypeCheckTag(VoidType.Instance);

            return new VoidLiteralSyntax<TypeCheckTag>(tag);
        }

        public ISyntax<TypeCheckTag> CheckBoolLiteral(BoolLiteralSyntax<ParseTag> literal, IScope scope, ITypeChecker checker) {
            var tag = new TypeCheckTag(BoolType.Instance);

            return new BoolLiteralSyntax<TypeCheckTag>(tag, literal.Value);
        }

        public ISyntax<TypeCheckTag> CheckBinarySyntax(BinarySyntax<ParseTag> syntax, IScope scope, ITypeChecker checker) {
            var left = checker.Check(syntax.Left, scope);
            var right = checker.Check(syntax.Right, scope);

            if (left.Tag.ReturnType == IntType.Instance) {
                if (right.Tag.ReturnType != IntType.Instance) {
                    throw TypeCheckingErrors.UnexpectedType(syntax.Right.Tag.Location, IntType.Instance, right.Tag.ReturnType);
                }

                if (!intOperations.TryGetValue(syntax.Kind, out var returnType)) {
                    throw TypeCheckingErrors.InvalidBinaryOperator(
                        syntax.Tag.Location, 
                        syntax.Kind, 
                        left.Tag.ReturnType);
                }

                var tag = new TypeCheckTag(
                    returnType,
                    left.Tag.CapturedVariables.Union(right.Tag.CapturedVariables));

                return new BinarySyntax<TypeCheckTag>(tag, syntax.Kind, left, right);
            }
            if (left.Tag.ReturnType == BoolType.Instance) {
                if (right.Tag.ReturnType != BoolType.Instance) {
                    throw TypeCheckingErrors.UnexpectedType(syntax.Right.Tag.Location, IntType.Instance, right.Tag.ReturnType);
                }

                if (!boolOperations.TryGetValue(syntax.Kind, out var returnType)) {
                    throw TypeCheckingErrors.InvalidBinaryOperator(
                        syntax.Tag.Location,
                        syntax.Kind,
                        left.Tag.ReturnType);
                }

                var tag = new TypeCheckTag(
                    returnType,
                    left.Tag.CapturedVariables.Union(right.Tag.CapturedVariables));

                return new BinarySyntax<TypeCheckTag>(tag, syntax.Kind, left, right);
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Left.Tag.Location, 
                    IntType.Instance, 
                    left.Tag.ReturnType);
            }            
        }

        public ISyntax<TypeCheckTag> CheckAlloc(AllocSyntax<ParseTag> syntax, IScope scope, ITypeChecker checker) {
            var target = checker.Check(syntax.Target, scope);
            var tag = new TypeCheckTag(
                new VariableType(target.Tag.ReturnType),
                target.Tag.CapturedVariables);

            return new AllocSyntax<TypeCheckTag>(tag, target);
        }
    }
}