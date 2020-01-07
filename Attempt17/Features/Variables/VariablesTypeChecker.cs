﻿using Attempt17.Features.Functions;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System.Collections.Immutable;

namespace Attempt17.Features.Variables {
    public class VariablesTypeChecker {
        public ISyntax<TypeCheckTag> CheckVariableAccess(VariableAccessParseSyntax syntax, Scope scope, ITypeChecker checker) {
            if (scope.FindVariable(syntax.VariableName).TryGetValue(out var info)) {
                if (syntax.Kind == VariableAccessKind.ValueAccess) {
                    ImmutableHashSet<IdentifierPath> captured;

                    // TODO - Make this more robust
                    if (info.Type is IntType || info.Type is VoidType) {
                        captured = ImmutableHashSet<IdentifierPath>.Empty;
                    }
                    else {
                        captured = new[] { info.Path }.ToImmutableHashSet();
                    }

                    var tag = new TypeCheckTag(info.Type, captured);

                    return new VariableAccessSyntax(tag, syntax.Kind, info);
                }
                else {
                    var tag = new TypeCheckTag(
                        new VariableType(info.Type), 
                        new[] { info.Path }.ToImmutableHashSet());

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

        public ISyntax<TypeCheckTag> CheckVariableInit(VariableInitSyntax<ParseTag> syntax, Scope scope, ITypeChecker checker) {
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
                    VariableSource.Alias,
                    path);
            }
            else {
                info = new VariableInfo(
                    value.Tag.ReturnType,
                    VariableSource.Local,
                    path);
            }

            // Add this variable to the current scope
            scope.Variables.Add(path, info);

            var tag = new TypeCheckTag(VoidType.Instance);

            return new VariableInitSyntax<TypeCheckTag>(
                tag, 
                syntax.VariableName, 
                syntax.Kind, 
                value);
        }

        public ISyntax<TypeCheckTag> CheckStore(StoreSyntax<ParseTag> syntax, Scope scope, ITypeChecker checker) {
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

            // Make sure the thing being stored will outlive these captured variables           
            foreach (var targetPath in target.Tag.CapturedVariables) {
                foreach (var valuePath in value.Tag.CapturedVariables) {
                    var targetScope = targetPath.Pop();
                    var valueScope = valuePath.Pop();

                    if (valueScope.StartsWith(targetScope) && valueScope != targetScope) {
                        throw TypeCheckingErrors.StoreScopeExceeded(syntax.Tag.Location, targetPath, valuePath);
                    }
                }
            }

            var tag = new TypeCheckTag(VoidType.Instance);

            return new StoreSyntax<TypeCheckTag>(tag, target, value);
        }
    }
}