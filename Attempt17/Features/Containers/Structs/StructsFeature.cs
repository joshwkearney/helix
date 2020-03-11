using System;
using Attempt17.Parsing;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Structs {
    public class StructsFeature : ILanguageFeature {
        private readonly StructsTypeChecker typeChecker = new StructsTypeChecker();
        private readonly StructsCodeGenerator codeGen = new StructsCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterDeclaration<StructDeclarationSyntax<ParseTag>>(this.typeChecker.ModifyScopeForStructDeclaration);
            registry.RegisterParseTree<StructDeclarationSyntax<ParseTag>>(this.typeChecker.CheckStructDeclaration);
            registry.RegisterSyntaxTree<StructDeclarationSyntax<TypeCheckTag>>(this.codeGen.GenerateStructDeclaration);

            registry.RegisterParseTree<NewStructSyntax<ParseTag>>(this.typeChecker.CheckNewStruct);
            registry.RegisterSyntaxTree<NewStructSyntax<TypeCheckTag>>(this.codeGen.GenerateNewStructSyntax);

            registry.RegisterSyntaxTree<StructMemberAccessSyntax>(this.codeGen.GenerateStructMemberAccess);
        }
    }
}