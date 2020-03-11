using Attempt17.Features.Functions;
using Attempt17.Features.Primitives;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt17.Features.Variables {
    public class VariablesTypeChecker {
        public ISyntax<TypeCheckTag> CheckVariableAccess(VariableAccessParseSyntax syntax, ITypeCheckScope scope, ITypeChecker checker) {
            if (scope.FindVariable(syntax.VariableName).TryGetValue(out var info)) {
                // Check that we're not accessing moved variables
                if (scope.IsVariableMoved(info.Path)) {
                    throw TypeCheckingErrors.AccessedMovedVariable(syntax.Tag.Location, info.Path);
                }

                if (syntax.Kind == VariableAccessKind.ValueAccess) {
                    var copiability = checker.GetTypeCopiability(info.VariableType, scope);
                    ImmutableHashSet<VariableCapture> captured;

                    if (copiability == TypeCopiability.Unconditional) {
                        captured = ImmutableHashSet<VariableCapture>.Empty;
                    }
                    else if (copiability == TypeCopiability.Conditional) {
                        var cap = new VariableCapture(VariableCaptureKind.ValueCapture, info.Path);
                        captured = new[] { cap }.ToImmutableHashSet();
                    }
                    else {
                        throw TypeCheckingErrors.TypeNotCopiable(syntax.Tag.Location, info.VariableType);
                    }

                    var tag = new TypeCheckTag(info.VariableType, captured);

                    return new VariableAccessSyntax(tag, syntax.Kind, info);
                }
                else {
                    var cap = new VariableCapture(VariableCaptureKind.IdentityCapture, info.Path);
                    var tag = new TypeCheckTag(
                        new VariableType(info.VariableType),
                        new[] { cap }.ToImmutableHashSet());

                    if (info.DefinitionKind == VariableDefinitionKind.Local && info.IsFunctionParameter) {
                        throw TypeCheckingErrors.AccessedFunctionParameterLikeVariable(syntax.Tag.Location, syntax.VariableName);
                    }

                    return new VariableAccessSyntax(tag, syntax.Kind, info);
                }
            }
            else if (scope.FindFunction(syntax.VariableName).TryGetValue(out var funcInfo)) {
                var tag = new TypeCheckTag(funcInfo.FunctionType);

                return new FunctionLiteralSyntax(tag);
            }
            else {
                throw TypeCheckingErrors.VariableUndefined(syntax.Tag.Location, syntax.VariableName);
            }
        }

        public ISyntax<TypeCheckTag> CheckVariableInit(VariableInitSyntax<ParseTag> syntax, ITypeCheckScope scope, ITypeChecker checker) {
            if (scope.IsNameTaken(syntax.VariableName)) {
                throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, syntax.VariableName);
            }

            var value = checker.Check(syntax.Value, scope);
            var path = scope.Path.Append(syntax.VariableName);

            VariableInfo info;

            // Make sure only var types are used in an equate
            if (syntax.Kind == VariableInitKind.Equate) {
                if (!(value.Tag.ReturnType is VariableType varType)) {
                    throw TypeCheckingErrors.ExpectedVariableType(syntax.Value.Tag.Location, value.Tag.ReturnType);
                }

                info = new VariableInfo(
                    varType.InnerType,
                    VariableDefinitionKind.Alias,
                    path);
            }
            else {
                info = new VariableInfo(
                    value.Tag.ReturnType,
                    VariableDefinitionKind.Local,
                    path);
            }

            // Add this variable to the current scope
            scope.SetTypeInfo(path, info);

            // Add this variable as a capturing variable to the other variables in this scope
            foreach (var cap in value.Tag.CapturedVariables) {
                scope.SetCapturingVariable(new VariableCapture(cap.Kind, path), cap.Path);
            }

            // Add the move-ability to the scope
            if (value is AllocSyntax<TypeCheckTag> || value.Tag.ReturnType is ArrayType) {
                scope.SetVariableMovable(info.Path, true);
            }

            var tag = new TypeCheckTag(VoidType.Instance);

            return new VariableInitSyntax<TypeCheckTag>(
                tag,
                syntax.VariableName,
                syntax.Kind,
                value);
        }

        public ISyntax<TypeCheckTag> CheckStore(StoreSyntax<ParseTag> syntax, ITypeCheckScope scope, ITypeChecker checker) {
            var target = checker.Check(syntax.Target, scope);
            var value = checker.Check(syntax.Value, scope);

            // Make sure the types match
            if (!(target.Tag.ReturnType is VariableType varType)) {
                throw TypeCheckingErrors.ExpectedVariableType(syntax.Target.Tag.Location, target.Tag.ReturnType);
            }

            // Make sure that equates have a variable type for the value
            if (varType.InnerType != value.Tag.ReturnType) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Value.Tag.Location,
                    varType.InnerType, value.Tag.ReturnType);
            }

            // Variables captured by the target collectively define the lifetime of 
            // the variable that is being stored into

            // Make sure that the variable being stored into was not value-captured
            // by something else, because that would allow the store to corrupt memory
            foreach (var capturedPath in target.Tag.CapturedVariables.Select(x => x.Path)) {
                var capturing = scope
                    .GetCapturingVariables(capturedPath)
                    .Where(x => x.Kind == VariableCaptureKind.ValueCapture)
                    .Select(x => x.Path)
                    .ToArray();

                if (capturing.Any()) {
                    throw TypeCheckingErrors.StoredToCapturedVariable(
                        syntax.Tag.Location,
                        capturedPath,
                        capturing.First());
                }
            }

            // Make sure the thing being stored will outlive these captured variables           
            foreach (var targetPath in target.Tag.CapturedVariables.Select(x => x.Path)) {
                foreach (var valuePath in value.Tag.CapturedVariables.Select(x => x.Path)) {
                    var targetScope = targetPath.Pop();
                    var valueScope = valuePath.Pop();

                    if (valueScope.StartsWith(targetScope) && valueScope != targetScope) {
                        throw TypeCheckingErrors.StoreScopeExceeded(syntax.Tag.Location, targetPath, valuePath);
                    }
                }
            }

            // Reset the move-ability of this variable to reflect the new value
            foreach (var targetPath in target.Tag.CapturedVariables.Select(x => x.Path)) {
                if (!(value is AllocSyntax<TypeCheckTag>)) {
                    scope.SetVariableMovable(targetPath, false);
                }
            }

            var tag = new TypeCheckTag(VoidType.Instance);

            return new StoreSyntax<TypeCheckTag>(tag, target, value);
        }

        public ISyntax<TypeCheckTag> CheckMove(MoveSyntax<ParseTag> syntax, ITypeCheckScope scope, ITypeChecker checker) {
            if (!scope.FindVariable(syntax.VariableName).TryGetValue(out var info)) {
                throw TypeCheckingErrors.VariableUndefined(syntax.Tag.Location, syntax.VariableName);
            }

            // Make sure the variable is movable
            if (!scope.IsVariableMovable(info.Path)) {
                throw TypeCheckingErrors.MovedUnmovableVariable(syntax.Tag.Location, info.Path);
            }

            // Make sure the variable is not already moved
            if (scope.IsVariableMoved(info.Path)) {
                throw TypeCheckingErrors.AccessedMovedVariable(syntax.Tag.Location, info.Path);
            }

            // Variables must not be captured to be moved
            var capturing = scope
                .GetCapturingVariables(info.Path)
                .Where(x => x.Kind == VariableCaptureKind.ValueCapture)
                .Select(x => x.Path)
                .ToArray();

            if (capturing.Any()) {
                throw TypeCheckingErrors.MovedCapturedVariable(syntax.Tag.Location, info.Path, capturing.First());
            }

            if (info.DefinitionKind == VariableDefinitionKind.Local) {
                if (syntax.Kind != MovementKind.ValueMove) {
                    throw TypeCheckingErrors.MovedUnmovableVariable(syntax.Tag.Location, info.Path);
                }

                scope.SetVariableMoved(info.Path, true);

                var tag = new TypeCheckTag(info.VariableType);

                return new MoveSyntax<TypeCheckTag>(tag, MovementKind.ValueMove, syntax.VariableName);
            }
            else if (info.DefinitionKind == VariableDefinitionKind.Alias) {
                if (syntax.Kind != MovementKind.LiteralMove) {
                    throw TypeCheckingErrors.MovedUnmovableVariable(syntax.Tag.Location, info.Path);
                }

                // Set the variable to moved
                scope.SetVariableMoved(info.Path, true);

                var tag = new TypeCheckTag(new VariableType(info.VariableType));

                return new MoveSyntax<TypeCheckTag>(tag, MovementKind.LiteralMove, syntax.VariableName);
            }
            else {
                throw new Exception("This should never happen");
            }
        }
    }
}