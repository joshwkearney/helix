using System;
using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers.Unions {
    public class UnionCodeGenerator : IUnionVisitor<CBlock, TypeCheckTag, CodeGenerationContext> {
        public CBlock VisitNewUnion(NewUnionSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            throw new NotImplementedException();
        }

        public CBlock VisitUnionDeclaration(UnionDeclarationSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            throw new NotImplementedException();
        }
    }
}
