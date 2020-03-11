using Attempt17.Parsing;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Functions {
    public class FunctionsFeature : ILanguageFeature {
        private readonly FunctionsTypeChecker typeChecker = new FunctionsTypeChecker();
        private readonly FunctionsCodeGenerator codeGen = new FunctionsCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterParseTree<ParseFunctionDeclaration>(this.typeChecker.CheckFunctionDeclaration);
            registry.RegisterDeclaration<ParseFunctionDeclaration>(this.typeChecker.ModifyDeclarationScope);
            registry.RegisterSyntaxTree<FunctionDeclarationSyntax>(this.codeGen.GenerateFunctionDeclaration);

            registry.RegisterParseTree<InvokeParseSyntax>(this.typeChecker.CheckInvoke);
            registry.RegisterSyntaxTree<InvokeSyntax>(this.codeGen.GenerateInvoke);

            registry.RegisterSyntaxTree<FunctionLiteralSyntax>(this.codeGen.GenerateFunctionLiteral);
        }
    }
}