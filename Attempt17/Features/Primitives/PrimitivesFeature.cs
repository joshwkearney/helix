﻿using Attempt17.Parsing;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Primitives {
    public class PrimitivesFeature : ILanguageFeature {
        private readonly PrimitivesTypeChecker typeChecker = new PrimitivesTypeChecker();
        private readonly PrimitivesCodeGenerator codeGen = new PrimitivesCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterParseTree<IntLiteralSyntax<ParseTag>>(this.typeChecker.CheckIntLiteral);
            registry.RegisterSyntaxTree<IntLiteralSyntax<TypeCheckTag>>(this.codeGen.GenerateIntLiteral);

            registry.RegisterParseTree<VoidLiteralSyntax<ParseTag>>(this.typeChecker.CheckVoidLiteral);
            registry.RegisterSyntaxTree<VoidLiteralSyntax<TypeCheckTag>>(this.codeGen.GenerateVoidLiteral);

            registry.RegisterParseTree<BoolLiteralSyntax<ParseTag>>(this.typeChecker.CheckBoolLiteral);
            registry.RegisterSyntaxTree<BoolLiteralSyntax<TypeCheckTag>>(this.codeGen.GenerateBoolLiteral);

            registry.RegisterParseTree<BinarySyntax<ParseTag>>(this.typeChecker.CheckBinarySyntax);
            registry.RegisterSyntaxTree<BinarySyntax<TypeCheckTag>>(this.codeGen.GenerateBinarySyntax);

            registry.RegisterParseTree<AllocSyntax<ParseTag>>(this.typeChecker.CheckAlloc);
            registry.RegisterSyntaxTree<AllocSyntax<TypeCheckTag>>(this.codeGen.GenerateAlloc);
        }
    }
}