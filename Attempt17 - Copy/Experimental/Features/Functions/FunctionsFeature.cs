using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.Functions {
    public class FunctionsFeature : ILanguageFeature {
        private readonly FunctionsTypeChecker typeChecker = new FunctionsTypeChecker();
        private readonly FunctionsCodeGenerator codeGen = new FunctionsCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterParseTree<FunctionDeclarationSyntax<ParseInfo>>(this.typeChecker.CheckFunctionDeclaration);
            registry.RegisterSyntaxTree<FunctionDeclarationSyntax<TypeCheckInfo>>(this.codeGen.GenerateFunctionDeclaration);
        }
    }
}