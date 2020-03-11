using System;
using Attempt17.Parsing;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Structs {
    public class StructsFeature : ILanguageFeature {
        private readonly StructsTypeChecker typeChecker = new StructsTypeChecker();
        private readonly StructsCodeGenerator codeGen = new StructsCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterDeclaration<StructDeclarationParseTree>(this.typeChecker.ModifyScopeForStructDeclaration);
            registry.RegisterParseTree<StructDeclarationParseTree>(this.typeChecker.CheckStructDeclaration);
            registry.RegisterSyntaxTree<StructDeclarationSyntaxTree>(this.codeGen.GenerateStructDeclaration);

            registry.RegisterParseTree<NewSyntax<ParseTag>>(this.typeChecker.CheckNew);
            registry.RegisterSyntaxTree<NewSyntax<TypeCheckTag>>(this.codeGen.GenerateNewSyntax);
        }
    }
}