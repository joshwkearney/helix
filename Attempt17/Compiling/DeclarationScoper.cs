using System;
using Attempt17.Features;
using Attempt17.Features.Containers.Composites;
using Attempt17.Features.Containers.Unions;
using Attempt17.Features.Functions;
using Attempt17.Parsing;
using Attempt17.TypeChecking;

namespace Attempt17.Compiling {
    public class DeclarationScoper : IDeclarationVisitor<IDeclaration<ParseTag>, ParseTag> {
        public IDeclaration<ParseTag> VisitCompositeDeclaration(
            CompositeDeclarationSyntax<ParseTag> syntax,
            ITypeCheckScope scope) {

            // Check to make sure the name isn't taken
            if (scope.IsPathTaken(syntax.CompositeInfo.Path)) {
                throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location,
                    syntax.CompositeInfo.Signature.Name);
            }

            // Add the struct or class to the scope
            scope.SetTypeInfo(syntax.CompositeInfo.Path, syntax.CompositeInfo);

            // Add each field into the scope
            foreach (var mem in syntax.CompositeInfo.Signature.Members) {
                var path = syntax.CompositeInfo.Path.Append(mem.Name);
                var info = new ReservedIdentifier(path, mem.Type);

                scope.SetTypeInfo(path, info);
            }

            return syntax;
        }

        public IDeclaration<ParseTag> VisitFunctionDeclaration(
            FunctionDeclarationSyntax<ParseTag> syntax,
            ITypeCheckScope scope) {

            // Check to make sure the name isn't taken
            if (scope.IsPathTaken(syntax.FunctionInfo.Path)) {
                throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location,
                    syntax.FunctionInfo.Signature.Name);
            }

            // Add the function to the scope
            scope.SetTypeInfo(syntax.FunctionInfo.Path, syntax.FunctionInfo);

            return syntax;
        }

        public IDeclaration<ParseTag> VisitUnionDeclaration(
            ParseUnionDeclarationSyntax<ParseTag> syntax,
            ITypeCheckScope scope) {

            // Check to make sure the name isn't taken
            if (scope.IsPathTaken(syntax.UnionInfo.Path)) {
                throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location,
                    syntax.UnionInfo.Signature.Name);
            }

            // Add the union to the scope
            scope.SetTypeInfo(syntax.UnionInfo.Path, syntax.UnionInfo);

            // Add each field into the scope
            foreach (var mem in syntax.UnionInfo.Signature.Members) {
                var path = syntax.UnionInfo.Path.Append(mem.Name);
                var info = new ReservedIdentifier(path, mem.Type);

                scope.SetTypeInfo(path, info);
            }

            // Add each method into the scope
            foreach (var method in syntax.Methods) {
                var path = syntax.UnionInfo.Path.Append(method.Name);

                var newSig = new FunctionSignature(
                    method.Name,
                    method.ReturnType,
                    method.Parameters.Insert(0, new Parameter("this", syntax.UnionInfo.Type)));

                var info = new FunctionInfo(path, newSig);

                scope.SetTypeInfo(path, info);
                scope.SetMethod(syntax.UnionInfo.Type, method.Name, path);
            }

            return syntax;
        }
    }
}
