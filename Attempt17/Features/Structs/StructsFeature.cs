using System;

namespace Attempt17.Features.Structs {
    public class StructsFeature : ILanguageFeature {
        private readonly StructsTypeChecker typeChecker = new StructsTypeChecker();
        private readonly StructsCodeGenerator codeGen = new StructsCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterDeclaration<StructDeclarationParseTree>(this.typeChecker.ModifyScopeForStructDeclaration);
            registry.RegisterParseTree<StructDeclarationParseTree>(this.typeChecker.CheckStructDeclaration);
            registry.RegisterSyntaxTree<StructDeclarationSyntaxTree>(this.codeGen.GenerateStructDeclaration);
        }
    }
}