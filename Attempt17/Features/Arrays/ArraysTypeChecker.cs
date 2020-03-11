using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt17.Features.Arrays {
    public class ArraysTypeChecker {
        public ISyntax<TypeCheckTag> CheckArrayRangeLiteral(ArrayRangeLiteralSyntax<ParseTag> syntax, IScope scope, ITypeChecker checker) {
            // Make sure the element type has a default value
            if (!syntax.ElementType.Accept(new TypeVoidValueVisitor(scope)).Any()) {
                throw TypeCheckingErrors.TypeWithoutDefaultValue(syntax.Tag.Location, syntax.ElementType);
            }

            var count = checker.Check(syntax.ElementCount, scope);

            // Make sure the index isn't out of range
            if (count.Tag.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(syntax.Tag.Location, IntType.Instance, count.Tag.ReturnType);
            }

            var tag = new TypeCheckTag(
                new ArrayType(syntax.ElementType), 
                count.Tag.CapturedVariables);

            return new ArrayRangeLiteralSyntax<TypeCheckTag>(
                tag, 
                syntax.ElementType, 
                count);
        }

        public ISyntax<TypeCheckTag> CheckArrayIndex(ArrayIndexSyntax<ParseTag> syntax, IScope scope, ITypeChecker checker) {
            var target = checker.Check(syntax.Target, scope);
            var index = checker.Check(syntax.Index, scope);

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
            var copiability = checker.GetTypeCopiability(arrType.ElementType, scope);
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

        public ISyntax<TypeCheckTag> CheckArrayStore(ArrayStoreSyntax<ParseTag> syntax, IScope scope, ITypeChecker checker) {
            var target = checker.Check(syntax.Target, scope);
            var index = checker.Check(syntax.Index, scope);
            var value = checker.Check(syntax.Value, scope);

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

        public ISyntax<TypeCheckTag> CheckArrayLiteral(ArrayLiteralSyntax<ParseTag> syntax, IScope scope, ITypeChecker checker) {
            var elements = syntax.Elements
                .Select(x => checker.Check(x, scope))
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
    }
}