using Attempt17.Parsing;
using Attempt17.TypeChecking;

namespace Attempt17.Features.FlowControl {
    public class FlowControlFeature : ILanguageFeature {
        private readonly FlowControlTypeChecker typeChecker = new FlowControlTypeChecker();

        private readonly FlowControlCodeGenerator codeGen = new FlowControlCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterParseTree<WhileSyntax<ParseTag>>(this.typeChecker.CheckWhileSyntax);
            registry.RegisterSyntaxTree<WhileSyntax<TypeCheckTag>>(this.codeGen.GenerateWhileSyntax);

            registry.RegisterParseTree<IfSyntax<ParseTag>>(this.typeChecker.CheckIfSyntax);
            registry.RegisterSyntaxTree<IfSyntax<TypeCheckTag>>(this.codeGen.GenerateIfSyntax);

            registry.RegisterParseTree<BlockSyntax<ParseTag>>(this.typeChecker.CheckBlockSyntax);
            registry.RegisterSyntaxTree<BlockSyntax<TypeCheckTag>>(this.codeGen.GenerateBlockSyntax);
        }
    }
}