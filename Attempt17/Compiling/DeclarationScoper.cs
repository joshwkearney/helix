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

            scope.SetTypeInfo(syntax.CompositeInfo.Path, syntax.CompositeInfo);

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

            scope.SetTypeInfo(syntax.FunctionInfo.Path, syntax.FunctionInfo);

            return syntax;
        }

        public IDeclaration<ParseTag> VisitUnionDeclaration(
            UnionDeclarationSyntax<ParseTag> syntax,
            ITypeCheckScope scope) {

            // Check to make sure the name isn't taken
            if (scope.IsPathTaken(syntax.UnionInfo.Path)) {
                throw TypeCheckingErrors.IdentifierDefined(syntax.Tag.Location,
                    syntax.UnionInfo.Signature.Name);
            }

            scope.SetTypeInfo(syntax.UnionInfo.Path, syntax.UnionInfo);

            return syntax;
        }
    }
}
