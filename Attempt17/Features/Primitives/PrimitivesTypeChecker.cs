using Attempt17.Features.Arrays;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System.Collections.Generic;
using System.Collections.Immutable;

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

        public ISyntax<TypeCheckTag> CheckAs(AsSyntax<ParseTag> syntax, IScope scope, ITypeChecker checker) {
            var target = checker.Check(syntax.Target, scope);

            if (!checker.Unify(target, scope, syntax.TargetType).TryGetValue(out var result)) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Tag.Location,
                    syntax.TargetType,
                    target.Tag.ReturnType);
            }

            return result;
        }

        public IOption<ISyntax<TypeCheckTag>> UnifyVoidToTypes(ISyntax<TypeCheckTag> syntax, IScope scope, LanguageType type) {
            if (syntax.Tag.ReturnType != VoidType.Instance) {
                return Option.None<ISyntax<TypeCheckTag>>();
            }

            if (type == IntType.Instance) {
                return Option.Some(
                    new IntLiteralSyntax<TypeCheckTag>(
                        new TypeCheckTag(type),
                        0L));
            }
            else if (type == VoidType.Instance) {
                return Option.Some(
                    new VoidLiteralSyntax<TypeCheckTag>(
                        new TypeCheckTag(type)));
            }
            else if (type == BoolType.Instance) {
                return Option.Some(
                    new BoolLiteralSyntax<TypeCheckTag>(
                        new TypeCheckTag(type),
                        false));
            }
            else if (type is ArrayType arrType) {
                // Make sure the elements have a default value
                if (arrType.ElementType.Accept(new TypeDefaultValueVisitor(scope))) {
                    return Option.Some(
                        new ArrayLiteralSyntax<TypeCheckTag>(
                            new TypeCheckTag(arrType),
                            ImmutableList<ISyntax<TypeCheckTag>>.Empty));
                }
            }

            return Option.None<ISyntax<TypeCheckTag>>();
        }
    }
}