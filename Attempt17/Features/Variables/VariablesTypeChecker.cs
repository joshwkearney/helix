using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Features.Functions;
using Attempt17.Features.Primitives;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Variables {
    public class VariablesTypeChecker
        : IVariablesVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> {

        public ISyntax<TypeCheckTag> VisitMove(MoveSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            if (!context.Scope.FindVariable(syntax.VariableName).TryGetValue(out var info)) {
                throw TypeCheckingErrors.VariableUndefined(syntax.Tag.Location,
                    syntax.VariableName);
            }

            // Make sure the variable is movable
            if (!context.Scope.IsVariableMovable(info.Path)) {
                throw TypeCheckingErrors.MovedUnmovableVariable(syntax.Tag.Location, info.Path);
            }

            // Make sure the variable is not already moved
            if (context.Scope.IsVariableMoved(info.Path)) {
                throw TypeCheckingErrors.AccessedMovedVariable(syntax.Tag.Location, info.Path);
            }

            // Variables must not be captured to be moved
            var capturing = context
                .Scope
                .GetCapturingVariables(info.Path)
                .Where(x => x.Kind == VariableCaptureKind.ValueCapture)
                .Select(x => x.Path)
                .ToArray();

            if (capturing.Any()) {
                throw TypeCheckingErrors.MovedCapturedVariable(syntax.Tag.Location, info.Path,
                    capturing.First());
            }

            if (info.DefinitionKind == VariableDefinitionKind.Local) {
                if (syntax.Kind != MovementKind.ValueMove) {
                    throw TypeCheckingErrors.MovedUnmovableVariable(syntax.Tag.Location, info.Path);
                }

                context.Scope.SetVariableMoved(info.Path, true);

                var tag = new TypeCheckTag(info.Type);

                return new MoveSyntax<TypeCheckTag>(tag, MovementKind.ValueMove,
                    syntax.VariableName);
            }
            else if (info.DefinitionKind == VariableDefinitionKind.Alias) {
                if (syntax.Kind != MovementKind.LiteralMove) {
                    throw TypeCheckingErrors.MovedUnmovableVariable(syntax.Tag.Location, info.Path);
                }

                // Set the variable to moved
                context.Scope.SetVariableMoved(info.Path, true);

                var tag = new TypeCheckTag(new VariableType(info.Type));

                return new MoveSyntax<TypeCheckTag>(tag, MovementKind.LiteralMove,
                    syntax.VariableName);
            }
            else {
                throw new Exception("This should never happen");
            }
        }

        public ISyntax<TypeCheckTag> VisitStore(StoreSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var target = syntax.Target.Accept(visitor, context);
            var value = syntax.Value.Accept(visitor, context);

            // Make sure the types match
            if (!(target.Tag.ReturnType is VariableType varType)) {
                throw TypeCheckingErrors.ExpectedVariableType(syntax.Target.Tag.Location,
                    target.Tag.ReturnType);
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
                var capturing = context
                    .Scope
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
                        throw TypeCheckingErrors.StoreScopeExceeded(syntax.Tag.Location,
                            targetPath, valuePath);
                    }
                }
            }

            // Reset the move-ability of this variable to reflect the new value
            // TODO: Figure out what this does exactly
            foreach (var targetPath in target.Tag.CapturedVariables.Select(x => x.Path)) {
                if (!(value is AllocSyntax<TypeCheckTag>)) {
                    context.Scope.SetVariableMovable(targetPath, false);
                }
            }

            var tag = new TypeCheckTag(VoidType.Instance);

            return new StoreSyntax<TypeCheckTag>(tag, target, value);
        }

        public ISyntax<TypeCheckTag> VisitVariableAccess(VariableAccessSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            throw new InvalidOperationException();
        }

        public ISyntax<TypeCheckTag> VisitVariableInit(VariableInitSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            if (context.Scope.IsNameTaken(syntax.VariableName)) {
                throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location,
                    syntax.VariableName);
            }

            var value = syntax.Value.Accept(visitor, context);
            var path = context.Scope.Path.Append(syntax.VariableName);

            VariableInfo info;

            // Make sure only var types are used in an equate
            if (syntax.Kind == VariableInitKind.Equate) {
                if (!(value.Tag.ReturnType is VariableType varType)) {
                    throw TypeCheckingErrors.ExpectedVariableType(syntax.Value.Tag.Location,
                        value.Tag.ReturnType);
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
            context.Scope.SetTypeInfo(path, info);

            // Add this variable as a capturing variable to the other variables in this scope
            foreach (var cap in value.Tag.CapturedVariables) {
                context.Scope.SetCapturingVariable(new VariableCapture(cap.Kind, path), cap.Path);
            }

            // Allocated variables are movable
            if (value is AllocSyntax<TypeCheckTag>) {
                context.Scope.SetVariableMovable(info.Path, true);
            }

            // Normally movable variables are also movable
            if (value.Tag.ReturnType.IsMovable(context.Scope)) {
                context.Scope.SetVariableMovable(info.Path, true);
            }

            var tag = new TypeCheckTag(VoidType.Instance);

            return new VariableInitSyntax<TypeCheckTag>(
                tag,
                syntax.VariableName,
                syntax.Kind,
                value);
        }

        public ISyntax<TypeCheckTag> VisitVariableParseAccess(
            VariableAccessParseSyntax<ParseTag> syntax,
            ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> visitor,
            TypeCheckContext context) {

            var varOp = context.Scope.FindVariable(syntax.VariableName);
            var funcOp = context.Scope.FindFunction(syntax.VariableName);

            if (varOp.TryGetValue(out var info)) {
                // Check that we're not accessing moved variables
                if (context.Scope.IsVariableMoved(info.Path)) {
                    throw TypeCheckingErrors.AccessedMovedVariable(syntax.Tag.Location, info.Path);
                }

                if (syntax.Kind == VariableAccessKind.ValueAccess) {
                    var copiability = info.Type.GetCopiability(context.Scope);
                    ImmutableHashSet<VariableCapture> captured;

                    if (copiability == TypeCopiability.Unconditional) {
                        captured = ImmutableHashSet<VariableCapture>.Empty;
                    }
                    else if (copiability == TypeCopiability.Conditional) {
                        var cap = new VariableCapture(VariableCaptureKind.ValueCapture, info.Path);
                        captured = new[] { cap }.ToImmutableHashSet();
                    }
                    else {
                        throw TypeCheckingErrors.TypeNotCopiable(syntax.Tag.Location, info.Type);
                    }

                    var tag = new TypeCheckTag(info.Type, captured);

                    return new VariableAccessSyntax<TypeCheckTag>(tag, syntax.Kind, info);
                }
                else {
                    var cap = new VariableCapture(VariableCaptureKind.IdentityCapture, info.Path);
                    var tag = new TypeCheckTag(
                        new VariableType(info.Type),
                        new[] { cap }.ToImmutableHashSet());

                    var badAccess = info.DefinitionKind == VariableDefinitionKind.Local
                        && info.IsFunctionParameter;

                    if (badAccess) {
                        throw TypeCheckingErrors.AccessedFunctionParameterLikeVariable(
                            syntax.Tag.Location,
                            syntax.VariableName);
                    }

                    return new VariableAccessSyntax<TypeCheckTag>(tag, syntax.Kind, info);
                }
            }
            else if (funcOp.TryGetValue(out var funcInfo)) {
                var tag = new TypeCheckTag(funcInfo.Type);

                return new FunctionLiteralSyntax<TypeCheckTag>(tag);
            }
            else {
                throw TypeCheckingErrors.VariableUndefined(syntax.Tag.Location,
                    syntax.VariableName);
            }
        }
    }
}
