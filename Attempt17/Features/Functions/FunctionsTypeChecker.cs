using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt17.Features.Functions {
    public class FunctionsTypeChecker {
        public ISyntax<TypeCheckTag> CheckFunctionDeclaration(FunctionDeclarationParseSyntax syntax, Scope scope, ITypeChecker checker) {
            if (!checker.IsTypeDefined(syntax.Signature.ReturnType, scope)) {
                throw TypeCheckingErrors.TypeUndefined(syntax.Tag.Location, syntax.Signature.ReturnType.ToString());
            }

            // Get a new scope for the function body
            var funcScope = scope.GetFrame(x => x.Append(syntax.Signature.Name));

            // Add each parameter to the scope
            foreach (var par in syntax.Signature.Parameters) {
                if (funcScope.IsNameTaken(par.Name)) {
                    throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, par.Name);
                }

                if (!checker.IsTypeDefined(par.Type, scope)) {
                    throw TypeCheckingErrors.TypeUndefined(syntax.Tag.Location, par.Type.ToString());
                }

                var path = funcScope.Path.Append(par.Name);
                var parInfo = new VariableInfo(par.Type, VariableSource.Local, path);

                funcScope.Variables.Add(path, parInfo);
            }

            var body = checker.Check(syntax.Body, funcScope);

            if (body.Tag.ReturnType != syntax.Signature.ReturnType) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Tag.Location, 
                    syntax.Signature.ReturnType, 
                    body.Tag.ReturnType);
            }

            var tag = new TypeCheckTag(VoidType.Instance);
            var info = new FunctionInfo(funcScope.Path, syntax.Signature);

            return new FunctionDeclarationSyntax(
                tag, 
                info, 
                body);
        }

        public void ModifyDeclarationScope(FunctionDeclarationParseSyntax syntax, Scope scope) {
            if (scope.IsNameTaken(syntax.Signature.Name)) {
                throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, syntax.Signature.Name);
            }

            var path = scope.Path.Append(syntax.Signature.Name);

            scope.Functions[path] = new FunctionInfo(path, syntax.Signature);
        }

        public ISyntax<TypeCheckTag> CheckInvoke(InvokeParseSyntax syntax, Scope scope, ITypeChecker checker) {
            var target = checker.Check(syntax.Target, scope);
            var args = syntax.Arguments
                .Select(x => checker.Check(x, scope))
                .ToImmutableList();

            if (!(target.Tag.ReturnType is NamedType namedType)) {
                throw TypeCheckingErrors.ExpectedFunctionType(syntax.Target.Tag.Location, target.Tag.ReturnType);
            }

            if (!scope.FindFunction(namedType.Path).TryGetValue(out var info)) {
                throw TypeCheckingErrors.ExpectedFunctionType(syntax.Target.Tag.Location, target.Tag.ReturnType);
            }

            if (info.Signature.Parameters.Count != args.Count) {
                throw TypeCheckingErrors.ParameterCountMismatch(syntax.Tag.Location, info.Signature.Parameters.Count, args.Count);
            }

            var zipped = args
                .Zip(info.Signature.Parameters, (x, y) => new {
                    ExpectedType = y.Type,
                    ActualType = x.Tag.ReturnType,
                })
                .Zip(syntax.Arguments, (x, y) => new {
                    x.ActualType,
                    x.ExpectedType,
                    y.Tag.Location
                });

            foreach (var item in zipped) {
                if (item.ExpectedType != item.ActualType) {
                    throw TypeCheckingErrors.UnexpectedType(item.Location, item.ExpectedType, item.ActualType);
                }
            }

            var captured = args.Aggregate(
                ImmutableHashSet<IdentifierPath>.Empty,
                (x, y) => x.Union(y.Tag.CapturedVariables));
            var tag = new TypeCheckTag(info.Signature.ReturnType, captured);

            return new InvokeSyntax(tag, info, args);
        }
    }
}
