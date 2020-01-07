using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.Primitives {
    public class PrimitivesFeature : ILanguageFeature {
        private readonly PrimitivesTypeChecker typeChecker = new PrimitivesTypeChecker();
        private readonly PrimitivesCodeGenerator codeGen = new PrimitivesCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterParseTree<IntLiteralSyntax<ParseInfo>>(this.typeChecker.CheckIntLiteral);
            registry.RegisterSyntaxTree<IntLiteralSyntax<TypeCheckInfo>>(this.codeGen.GenerateIntLiteral);

            registry.RegisterParseTree<VoidLiteralSyntax<ParseInfo>>(this.typeChecker.CheckVoidLiteral);
            registry.RegisterSyntaxTree<VoidLiteralSyntax<TypeCheckInfo>>(this.codeGen.GenerateVoidLiteral);

            registry.RegisterParseTree<BinarySyntax<ParseInfo>>(this.typeChecker.CheckBinarySyntax);
            registry.RegisterSyntaxTree<BinarySyntax<TypeCheckInfo>>(this.codeGen.GenerateBinarySyntax);
        }
    }
}