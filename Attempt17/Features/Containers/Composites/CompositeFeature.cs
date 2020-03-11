using System;
using Attempt17.Parsing;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Composites {
    public class CompositeFeature : ILanguageFeature {
        private readonly CompositeTypeChecker typeChecker = new CompositeTypeChecker();
        private readonly CompositeCodeGenerator codeGen = new CompositeCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterDeclaration<CompositeDeclarationSyntax<ParseTag>>(this.typeChecker.ModifyScopeForCompositeDeclaration);
            registry.RegisterParseTree<CompositeDeclarationSyntax<ParseTag>>(this.typeChecker.CheckCompositeDeclaration);
            registry.RegisterSyntaxTree<CompositeDeclarationSyntax<TypeCheckTag>>(this.codeGen.GenerateCompositeDeclaration);

            registry.RegisterParseTree<NewCompositeSyntax<ParseTag>>(this.typeChecker.CheckNewComposite);
            registry.RegisterSyntaxTree<NewCompositeSyntax<TypeCheckTag>>(this.codeGen.GenerateNewCompositeSyntax);

            registry.RegisterSyntaxTree<CompositeMemberAccessSyntax>(this.codeGen.GenerateCompositeMemberAccess);
        }
    }
}