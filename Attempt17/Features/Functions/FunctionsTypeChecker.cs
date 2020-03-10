using Attempt17.Features.FlowControl;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt17.Features.Functions {
    public class FunctionsTypeChecker {
        public ISyntax<TypeCheckTag> CheckFunctionDeclaration(FunctionDeclarationParseSyntax syntax, IScope scope, ITypeChecker checker) {
            // Make sure the return type is defined
            if (!checker.IsTypeDefined(syntax.Signature.ReturnType, scope)) {
                throw TypeCheckingErrors.TypeUndefined(syntax.Tag.Location, syntax.Signature.ReturnType.ToString());
            }

            // Get a new scope for the function body
            var funcScope = new BlockScope(scope.Path.Append(syntax.Signature.Name), scope);

            // Add each parameter to the scope
            foreach (var par in syntax.Signature.Parameters) {
                // Make sure the name is availible
                if (funcScope.IsNameTaken(par.Name)) {
                    throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, par.Name);
                }

                // Make sure the type is defined
                if (!checker.IsTypeDefined(par.Type, scope)) {
                    throw TypeCheckingErrors.TypeUndefined(syntax.Tag.Location, par.Type.ToString());
                }

                var path = funcScope.Path.Append(par.Name);
                VariableInfo parInfo;

                // Get the correct variable type, based on if it's a var type or not
                if (par.Type is VariableType varType) {
                    parInfo = new VariableInfo(
                        varType.InnerType, 
                        VariableDefinitionKind.Alias, 
                        path,
                        true);
                }
                else {
                    parInfo = new VariableInfo(
                        par.Type,
                        VariableDefinitionKind.Local,
                        path,
                        true);
                }

                // Add the variable to the scope
                funcScope.SetTypeInfo(path, parInfo);

                if (par.Type is VariableType || par.Type is ArrayType) {
                    funcScope.SetVariableMovable(path, true);
                }
            }
            
            var body = checker.Check(syntax.Body, funcScope);

            // Return types have to match
            if (body.Tag.ReturnType != syntax.Signature.ReturnType) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Tag.Location, 
                    syntax.Signature.ReturnType, 
                    body.Tag.ReturnType);
            }

            // Make sure we're not about to return a value that's dependent on variables
            // within this scope
            foreach (var path in body.Tag.CapturedVariables.Select(x => x.Path)) {
                if (path.StartsWith(funcScope.Path)) {
                    throw TypeCheckingErrors.VariableScopeExceeded(syntax.Body.Tag.Location, path);
                }
            }

            var tag = new TypeCheckTag(VoidType.Instance);
            var info = new FunctionInfo(funcScope.Path, syntax.Signature);

            return new FunctionDeclarationSyntax(
                tag, 
                info, 
                body);
        }

        public void ModifyDeclarationScope(FunctionDeclarationParseSyntax syntax, IScope scope) {
            var path = scope.Path.Append(syntax.Signature.Name);

            if (scope.IsPathTaken(path)) {
                throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, syntax.Signature.Name);
            }

            scope.SetTypeInfo(path, new FunctionInfo(path, syntax.Signature));
        }

        public ISyntax<TypeCheckTag> CheckInvoke(InvokeParseSyntax syntax, IScope scope, ITypeChecker checker) {
            var target = checker.Check(syntax.Target, scope);
            var args = syntax.Arguments
                .Select(x => checker.Check(x, scope))
                .ToImmutableList();

            // Make sure the target is a named type
            if (!(target.Tag.ReturnType is NamedType namedType)) {
                throw TypeCheckingErrors.ExpectedFunctionType(syntax.Target.Tag.Location, target.Tag.ReturnType);
            }

            // Make sure that named type is a function
            if (!scope.FindFunction(namedType.Path).TryGetValue(out var info)) {
                throw TypeCheckingErrors.ExpectedFunctionType(syntax.Target.Tag.Location, target.Tag.ReturnType);
            }

            // Make sure the parameter counts match
            if (info.Signature.Parameters.Count != args.Count) {
                throw TypeCheckingErrors.ParameterCountMismatch(syntax.Tag.Location, info.Signature.Parameters.Count, args.Count);
            }

            var zipped = args
                .Zip(info.Signature.Parameters, (x, y) => new {
                    ExpectedType = y.Type,
                    ActualType = x.Tag.ReturnType,
                    y.Name,
                    Value = x
                })
                .Zip(syntax.Arguments, (x, y) => new {
                    x.ActualType,
                    x.ExpectedType,
                    y.Tag.Location,
                    x.Name,
                    x.Value
                })
                .ToArray();

            // Make sure the parameter types match
            foreach (var item in zipped) {
                if (item.ExpectedType != item.ActualType) {
                    throw TypeCheckingErrors.UnexpectedType(item.Location, item.ExpectedType, item.ActualType);
                }
            }
            
            // Make sure that one variable might not be mutated by another into an invalid state
            foreach (var outerPar in zipped) {
                foreach (var innerPar in zipped) {
                    var outerCaptured = outerPar.Value.Tag.CapturedVariables;
                    var innerCaptured = innerPar.Value.Tag.CapturedVariables;

                    foreach (var outer in outerCaptured) {
                        foreach (var inner in innerCaptured) {
                            var outerScope = outer.Path.Pop();
                            var innerScope = inner.Path.Pop();

                            // Outer is not actually outside of inner
                            if (outerScope == innerScope || !innerScope.StartsWith(outerScope)) {
                                continue;
                            }

                            // Now that we know that inner will be destructed before outer, we have to
                            // confirm that outer cannot be mutated by something within inner

                            var mutators = outerPar.ActualType.Accept(new TypeMutatorsVisitor(scope));
                            var accessableTypes = innerPar.ActualType.Accept(new AccessibleTypesVisitor(scope));

                            // Outer can be mutated by inner, so throw
                            if (mutators.Intersect(accessableTypes).Any()) {
                                throw TypeCheckingErrors.PossibleInvalidParamMutation(
                                    syntax.Tag.Location,
                                    outer.Path,
                                    inner.Path);
                            }
                        }
                    }                 
                }
            }

            // To figure out which parameters the return type is dependent on, we need to
            // get all the types that are accessible from the return type. However, we
            // don't care about any types that are unconditionally copiable, these can
            // come from anywhere. Only conditionally copiable and non-copiable types
            // might include the parameters

            var returnAccessible = info.Signature
                .ReturnType
                .Accept(new AccessibleTypesVisitor(scope))
                .Where(x => x.Accept(new TypeCopiabilityVisitor(scope)) != TypeCopiability.Unconditional)
                .ToImmutableHashSet();

            // Now, capture all paths from any paramter that could be included in the
            // return type

            var captured = args
                .Where(x => {
                    var parAccessible = x.Tag.ReturnType.Accept(new AccessibleTypesVisitor(scope));

                    return returnAccessible.Intersect(parAccessible).Any();
                })
                .Select(x => x.Tag.CapturedVariables)
                .Aggregate(ImmutableHashSet<VariableCapture>.Empty, (x, y) => x.Union(y));

            var tag = new TypeCheckTag(info.Signature.ReturnType, captured);

            return new InvokeSyntax(tag, info, args);
        }
    }
}
