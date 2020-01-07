using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.Variables {
    public class VariablesFeature : ILanguageFeature {
        private readonly VariablesTypeChecker typeChecker = new VariablesTypeChecker();
        private readonly VariablesCodeGenerator codeGen = new VariablesCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterParseTree<VariableAccessParseSyntax>(this.typeChecker.CheckVariableAccess);
            registry.RegisterSyntaxTree<VariableAccessSyntax>(this.codeGen.GenerateVariableAccess);

            registry.RegisterParseTree<VariableInitSyntax<ParseInfo>>(this.typeChecker.CheckVariableInit);
            registry.RegisterSyntaxTree<VariableInitSyntax<TypeCheckInfo>>(this.codeGen.GenerateVariableInit);

            registry.RegisterParseTree<StoreSyntax<ParseInfo>>(this.typeChecker.CheckStore);
            registry.RegisterSyntaxTree<StoreSyntax<TypeCheckInfo>>(this.codeGen.GenerateStore);
        }
    }
}