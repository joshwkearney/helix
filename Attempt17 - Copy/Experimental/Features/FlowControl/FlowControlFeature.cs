using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.FlowControl {
    public class FlowControlFeature : ILanguageFeature {
        private readonly FlowControlTypeChecker typeChecker = new FlowControlTypeChecker();

        private readonly FlowControlCodeGenerator codeGen = new FlowControlCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterParseTree<WhileSyntax<ParseInfo>>(this.typeChecker.CheckWhileSyntax);
            registry.RegisterSyntaxTree<WhileSyntax<TypeCheckInfo>>(this.codeGen.GenerateWhileSyntax);

            registry.RegisterParseTree<IfSyntax<ParseInfo>>(this.typeChecker.CheckIfSyntax);
            registry.RegisterSyntaxTree<IfSyntax<TypeCheckInfo>>(this.codeGen.GenerateIfSyntax);

            registry.RegisterParseTree<BlockSyntax<ParseInfo>>(this.typeChecker.CheckBlockSyntax);
            registry.RegisterSyntaxTree<BlockSyntax<TypeCheckInfo>>(this.codeGen.GenerateBlockSyntax);
        }
    }
}