using System;
using Attempt17.CodeGeneration;
using Attempt17.Features.Containers.Arrays;
using Attempt17.Features.Containers.Composites;
using Attempt17.Features.Containers.Unions;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Containers {
    public class ContainersCodeGenerator
        : IContainersVisitor<CBlock, TypeCheckTag, CodeGenerationContext> {

        public
        IArraysVisitor<CBlock, TypeCheckTag, CodeGenerationContext> ArraysVisitor { get; }
            = new ArraysCodeGenerator();

        public
        ICompositesVisitor<CBlock, TypeCheckTag, CodeGenerationContext> CompositesVisitor { get; }
            = new CompositesCodeGenerator();

        public
        IUnionVisitor<CBlock, TypeCheckTag, CodeGenerationContext> UnionVisitor { get; }
            = new UnionCodeGenerator();

        public CBlock VisitMemberUsage(MemberUsageSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            throw new InvalidOperationException();
        }

        public CBlock VisitNew(NewSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            throw new InvalidOperationException();
        }
    }
}
