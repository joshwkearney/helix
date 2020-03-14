using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Features;
using Attempt17.Features.Containers.Composites;
using Attempt17.Features.Containers.Unions;
using Attempt17.Features.Functions;
using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Compiling {
    public class DeclarationFlattener
        : IDeclarationVisitor<IEnumerable<IDeclaration<ParseTag>>, ParseTag> {

        private readonly ITypeCheckScope scope;

        public DeclarationFlattener(ITypeCheckScope scope) {
            this.scope = scope;
        }

        public IEnumerable<IDeclaration<ParseTag>> VisitFunctionDeclaration(
            FunctionDeclarationSyntax<ParseTag> decl, ITypeCheckScope scope) {

            return new[] {
                new FunctionDeclarationSyntax<ParseTag>(decl.Tag, decl.FunctionInfo, decl.Body)
            };
        }

        public IEnumerable<IDeclaration<ParseTag>> VisitCompositeDeclaration(
            CompositeDeclarationSyntax<ParseTag> decl, ITypeCheckScope scope) {

            var transformer = new StructDeclarationTransformer(decl.CompositeInfo.Path,
                this.scope);

            var newDecl = new CompositeDeclarationSyntax<ParseTag>(
                decl.Tag,
                decl.CompositeInfo,
                ImmutableList<IDeclaration<ParseTag>>.Empty);

            return decl
                .InnerDeclarations
                .SelectMany(x => x.Accept(transformer, scope).Accept(this, scope))
                .Prepend(newDecl)
                .ToArray();
        }

        public IEnumerable<IDeclaration<ParseTag>> VisitUnionDeclaration(
            UnionDeclarationSyntax<ParseTag> decl, ITypeCheckScope scope) {

            throw new System.NotImplementedException();
        }
    }

    public class StructDeclarationTransformer
        : IDeclarationVisitor<IDeclaration<ParseTag>, ParseTag> {

        private readonly IdentifierPath containingStruct;
        private readonly ITypeCheckScope scope;

        public StructDeclarationTransformer(IdentifierPath containingStruct,
            ITypeCheckScope scope) {

            this.containingStruct = containingStruct;
            this.scope = scope;
        }

        public IDeclaration<ParseTag> VisitFunctionDeclaration(
            FunctionDeclarationSyntax<ParseTag> decl, ITypeCheckScope scope) {

            var structType = new NamedType(this.containingStruct);
            var firstParam = new Parameter("this", structType);

            var newSig = new FunctionSignature(
                decl.FunctionInfo.Signature.Name,
                decl.FunctionInfo.Signature.ReturnType,
                decl.FunctionInfo.Signature.Parameters.Insert(0, firstParam));

            var newInfo = new FunctionInfo(
                this.containingStruct.Append(decl.FunctionInfo.Path), newSig);

            this.scope.SetMethod(structType, decl.FunctionInfo.Signature.Name, newInfo.Path);

            return new FunctionDeclarationSyntax<ParseTag>(decl.Tag, newInfo, decl.Body);
        }

        public IDeclaration<ParseTag> VisitCompositeDeclaration(
            CompositeDeclarationSyntax<ParseTag> decl, ITypeCheckScope scope) {

            var newInfo = new CompositeInfo(
                decl.CompositeInfo.Signature,
                this.containingStruct.Append(decl.CompositeInfo.Path),
                decl.CompositeInfo.Kind);

            return new CompositeDeclarationSyntax<ParseTag>(decl.Tag, newInfo,
                decl.InnerDeclarations);
        }

        public IDeclaration<ParseTag> VisitUnionDeclaration(UnionDeclarationSyntax<ParseTag> decl,
            ITypeCheckScope scope) {

            throw new System.NotImplementedException();
        }
    }
}