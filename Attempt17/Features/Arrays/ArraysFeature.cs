using Attempt17.Parsing;
using Attempt17.TypeChecking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Arrays {
    public class ArraysFeature : ILanguageFeature {
        private readonly ArraysTypeChecker typeChecker = new ArraysTypeChecker();
        private readonly ArraysCodeGenerator codeGen = new ArraysCodeGenerator();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            registry.RegisterParseTree<ArrayRangeLiteralSyntax<ParseTag>>(this.typeChecker.CheckArrayRangeLiteral);
            registry.RegisterSyntaxTree<ArrayRangeLiteralSyntax<TypeCheckTag>>(this.codeGen.GenerateArrayRangeLiteral);

            registry.RegisterParseTree<ArrayIndexSyntax<ParseTag>>(this.typeChecker.CheckArrayIndex);
            registry.RegisterSyntaxTree<ArrayIndexSyntax<TypeCheckTag>>(this.codeGen.GenerateArrayIndex);

            registry.RegisterParseTree<ArrayStoreSyntax<ParseTag>>(this.typeChecker.CheckArrayStore);
            registry.RegisterSyntaxTree<ArrayStoreSyntax<TypeCheckTag>>(this.codeGen.GenerateArrayStore);

            registry.RegisterParseTree<ArrayLiteralSyntax<ParseTag>>(this.typeChecker.CheckArrayLiteral);
            registry.RegisterSyntaxTree<ArrayLiteralSyntax<TypeCheckTag>>(this.codeGen.GenerateArrayLiteral);

            registry.RegisterParseTree<MemberAccessSyntax<ParseTag>>(this.typeChecker.CheckMemberAccess);
            registry.RegisterSyntaxTree<MemberAccessSyntax<TypeCheckTag>>(this.codeGen.GenerateMemberAccess);
        }
    }
}