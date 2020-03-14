using System;
using System.Collections.Generic;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Primitives {
    public class PrimitivesTypeChecker
        : IPrimitivesVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> {

        private readonly Dictionary<BinarySyntaxKind, LanguageType> intOperations
            = new Dictionary<BinarySyntaxKind, LanguageType>() {

            { BinarySyntaxKind.Add, IntType.Instance },
            { BinarySyntaxKind.Subtract, IntType.Instance },
            { BinarySyntaxKind.Multiply, IntType.Instance },
            { BinarySyntaxKind.And, IntType.Instance },
            { BinarySyntaxKind.Or, IntType.Instance },
            { BinarySyntaxKind.Xor, IntType.Instance },
            { BinarySyntaxKind.EqualTo, BoolType.Instance },
            { BinarySyntaxKind.NotEqualTo, BoolType.Instance },
            { BinarySyntaxKind.GreaterThan, BoolType.Instance },
            { BinarySyntaxKind.LessThan, BoolType.Instance },
            { BinarySyntaxKind.GreaterThanOrEqualTo, BoolType.Instance },
            { BinarySyntaxKind.LessThanOrEqualTo, BoolType.Instance },
        };

        private readonly Dictionary<BinarySyntaxKind, LanguageType> boolOperations
            = new Dictionary<BinarySyntaxKind, LanguageType>() {

            { BinarySyntaxKind.And, BoolType.Instance },
            { BinarySyntaxKind.Or, BoolType.Instance },
                { BinarySyntaxKind.Xor, BoolType.Instance },
            { BinarySyntaxKind.EqualTo, BoolType.Instance },
                { BinarySyntaxKind.NotEqualTo, BoolType.Instance },
        };

        public ISyntax<TypeCheckTag> VisitAlloc(AllocSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var target = syntax.Target.Accept(visitor, context);
            var tag = new TypeCheckTag(
                new VariableType(target.Tag.ReturnType),
                target.Tag.CapturedVariables);

            return new AllocSyntax<TypeCheckTag>(tag, target);
        }

        public ISyntax<TypeCheckTag> VisitAs(AsSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var target = syntax
                .Target
                .Accept(visitor, context)
                .UnifyTo(syntax.TargetType, syntax.Tag.Location, context.Scope);

            return target;
        }

        public ISyntax<TypeCheckTag> VisitBinary(BinarySyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var left = syntax.Left.Accept(visitor, context);
            var right = syntax.Right.Accept(visitor, context);

            if (left.Tag.ReturnType == IntType.Instance) {
                if (right.Tag.ReturnType != IntType.Instance) {
                    throw TypeCheckingErrors.UnexpectedType(syntax.Right.Tag.Location,
                        IntType.Instance, right.Tag.ReturnType);
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
                    throw TypeCheckingErrors.UnexpectedType(syntax.Right.Tag.Location,
                        IntType.Instance, right.Tag.ReturnType);
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

        public ISyntax<TypeCheckTag> VisitBoolLiteral(BoolLiteralSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var tag = new TypeCheckTag(BoolType.Instance);

            return new BoolLiteralSyntax<TypeCheckTag>(tag, syntax.Value);
        }

        public ISyntax<TypeCheckTag> VisitIntLiteral(IntLiteralSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var tag = new TypeCheckTag(IntType.Instance);

            return new IntLiteralSyntax<TypeCheckTag>(tag, syntax.Value);
        }

        public ISyntax<TypeCheckTag> VisitVoidLiteral(VoidLiteralSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var tag = new TypeCheckTag(VoidType.Instance);

            return new VoidLiteralSyntax<TypeCheckTag>(tag);
        }
    }
}