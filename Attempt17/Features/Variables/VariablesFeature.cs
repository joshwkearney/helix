using Attempt17.Parsing;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Variables {
    public class VariablesFeature : ILanguageFeature {
        private readonly VariablesTypeChecker typeChecker = new VariablesTypeChecker();
        private readonly VariablesCodeGenerator codeGen = new VariablesCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterParseTree<VariableAccessParseSyntax>(this.typeChecker.CheckVariableAccess);
            registry.RegisterSyntaxTree<VariableAccessSyntax>(this.codeGen.GenerateVariableAccess);

            registry.RegisterParseTree<VariableInitSyntax<ParseTag>>(this.typeChecker.CheckVariableInit);
            registry.RegisterSyntaxTree<VariableInitSyntax<TypeCheckTag>>(this.codeGen.GenerateVariableInit);

            registry.RegisterParseTree<StoreSyntax<ParseTag>>(this.typeChecker.CheckStore);
            registry.RegisterSyntaxTree<StoreSyntax<TypeCheckTag>>(this.codeGen.GenerateStore);

            registry.RegisterParseTree<MoveSyntax<ParseTag>>(this.typeChecker.CheckMove);
            registry.RegisterSyntaxTree<MoveSyntax<TypeCheckTag>>(this.codeGen.GenerateMove);
        }
    }
}