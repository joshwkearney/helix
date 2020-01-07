using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt17.Experimental.Features.Functions {
    public class FunctionsTypeChecker {
        public ISyntax<TypeCheckInfo> CheckFunctionDeclaration(FunctionDeclarationSyntax<ParseInfo> syntax, Scope scope, ITypeChecker checker) {
            if (scope.IsNameTaken(syntax.Signature.Name)) {
                throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, syntax.Signature.Name);
            }

            // TODO - Fix this
            //if (!syntax.Signature.ReturnType.IsDefinedWithin(scope)) {
            //    throw TypeCheckingErrors.TypeUndefined(syntax.Tag.Location, syntax.Signature.ReturnType.ToString());
            //}

            // Get a new scope for the function body
            var funcScope = scope.GetFrame(x => x.Append(syntax.Signature.Name));

            // Add each parameter to the scope
            foreach (var par in syntax.Signature.Parameters) {
                if (funcScope.IsNameTaken(par.Name)) {
                    throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location, par.Name);
                }

                // TODO - Fix this
                //if (!par.Type.IsDefinedWithin(scope)) {
                //    throw TypeCheckingErrors.TypeUndefined(syntax.Location, par.Type.ToString());
                //}

                var path = funcScope.Path.Append(par.Name);
                var info = new VariableInfo(par.Type, VariableSource.Local, path);

                funcScope.Variables.Add(path, info);
            }

            var body = checker.Check(syntax.Body, funcScope);

            if (body.Tag.ReturnType != syntax.Signature.ReturnType) {
                throw TypeCheckingErrors.UnexpectedType(
                    syntax.Tag.Location, 
                    syntax.Signature.ReturnType, 
                    body.Tag.ReturnType);
            }

            var tag = new TypeCheckInfo(VoidType.Instance);

            return new FunctionDeclarationSyntax<TypeCheckInfo>(
                tag, 
                syntax.Signature, 
                body);
        }

        public ISyntax<TypeCheckInfo> CheckInvoke(InvokeParseSyntax syntax, Scope scope, ITypeChecker checker) {
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
            var tag = new TypeCheckInfo(info.Signature.ReturnType, captured);

            return new InvokeSyntax(tag, info, args);
        }
    }
}
