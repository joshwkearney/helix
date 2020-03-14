using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.TypeChecking.Unifiers;
using Attempt17.Types;

namespace Attempt17.Features.Containers.Arrays {
    public class ArraysTypeChecker
        : IArraysVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> {

        public ISyntax<TypeCheckTag> VisitArrayRangeLiteral(
            ArrayRangeLiteralSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            // Make sure the element type has a default value
            if (!syntax.ElementType.Accept(new FromVoidUnifier(context.Scope)).Any()) {
                throw TypeCheckingErrors.TypeWithoutDefaultValue(syntax.Tag.Location,
                    syntax.ElementType);
            }

            var count = syntax.ElementCount.Accept(visitor, context);

            // Make sure the index isn't out of range
            if (count.Tag.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(syntax.Tag.Location,
                    IntType.Instance, count.Tag.ReturnType);
            }

            var tag = new TypeCheckTag(
                new ArrayType(syntax.ElementType),
                count.Tag.CapturedVariables);

            return new ArrayRangeLiteralSyntax<TypeCheckTag>(
                tag,
                syntax.ElementType,
                count);
        }

        public ISyntax<TypeCheckTag> VisitIndex(ArrayIndexSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var target = syntax.Target.Accept(visitor, context);
            var index = syntax.Index.Accept(visitor, context);

            // Make sure the target is an array
            if (!(target.Tag.ReturnType is ArrayType arrType)) {
                throw TypeCheckingErrors.ExpectedArrayType(
                    syntax.Target.Tag.Location,
                    target.Tag.ReturnType);
            }

            // Make sure the index is an integer
            if (index.Tag.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Index.Tag.Location,
                    IntType.Instance,
                    index.Tag.ReturnType);
            }

            // Get the correct closed variables
            var copiability = arrType.ElementType.GetCopiability(context.Scope);
            ImmutableHashSet<VariableCapture> captured;

            if (copiability == TypeCopiability.Unconditional) {
                // Unconditional copying means that no variables are captured
                captured = ImmutableHashSet<VariableCapture>.Empty;
            }
            else if (copiability == TypeCopiability.Conditional) {
                // Conditional copying means that any variables captured by the target
                // syntax must also be captured by the result
                captured = target.Tag.CapturedVariables;
            }
            else {
                throw TypeCheckingErrors.TypeNotCopiable(syntax.Tag.Location, arrType.ElementType);
            }

            var tag = new TypeCheckTag(
                arrType.ElementType,
                captured);

            return new ArrayIndexSyntax<TypeCheckTag>(tag, target, index);
        }

        public ISyntax<TypeCheckTag> VisitLiteral(ArrayLiteralSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var elements = syntax.Elements
                .Select(x => x.Accept(visitor, context))
                .ToImmutableList();

            if (!elements.Any()) {
                throw TypeCheckingErrors.ZeroLengthArrayLiteral(syntax.Tag.Location);
            }

            var type = elements.First().Tag.ReturnType;

            foreach (var elem in elements) {
                if (elem.Tag.ReturnType != type) {
                    throw TypeCheckingErrors.UnexpectedType(
                        syntax.Tag.Location,
                        type,
                        elem.Tag.ReturnType);
                }
            }

            var cap = elements.Aggregate(
                ImmutableHashSet<VariableCapture>.Empty,
                (x, y) => x.Union(y.Tag.CapturedVariables));
            var tag = new TypeCheckTag(new ArrayType(type), cap);

            return new ArrayLiteralSyntax<TypeCheckTag>(tag, elements);
        }

        public ISyntax<TypeCheckTag> VisitSizeAccess(ArraySizeAccessSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            throw new InvalidOperationException();
        }

        public ISyntax<TypeCheckTag> VisitStore(ArrayStoreSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var target = syntax.Target.Accept(visitor, context);
            var index = syntax.Index.Accept(visitor, context);
            var value = syntax.Value.Accept(visitor, context);

            // Make sure we've got an array
            if (!(target.Tag.ReturnType is ArrayType arrType)) {
                throw TypeCheckingErrors.ExpectedArrayType(
                    syntax.Target.Tag.Location,
                    target.Tag.ReturnType);
            }

            // Make sure the index is an int
            if (index.Tag.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Index.Tag.Location,
                    IntType.Instance,
                    index.Tag.ReturnType);
            }

            // Make sure the value matches the array type
            if (value.Tag.ReturnType != arrType.ElementType) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Value.Tag.Location,
                    arrType.ElementType,
                    value.Tag.ReturnType);
            }

            // Make sure the thing being stored will outlive the array        
            foreach (var targetPath in target.Tag.CapturedVariables.Select(x => x.Path)) {
                foreach (var valuePath in value.Tag.CapturedVariables.Select(x => x.Path)) {
                    var targetScope = targetPath.Pop();
                    var valueScope = valuePath.Pop();

                    if (valueScope.StartsWith(targetScope) && valueScope != targetScope) {
                        throw TypeCheckingErrors.StoreScopeExceeded(syntax.Tag.Location, targetPath, valuePath);
                    }
                }
            }

            var tag = new TypeCheckTag(
                VoidType.Instance,
                target.Tag.CapturedVariables
                    .Union(index.Tag.CapturedVariables)
                    .Union(value.Tag.CapturedVariables));

            return new ArrayStoreSyntax<TypeCheckTag>(tag, target, index, value);
        }
    }
}

