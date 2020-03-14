using System;
using Attempt17.CodeGeneration;
using Attempt17.Features.Containers;
using Attempt17.Features.FlowControl;
using Attempt17.Features.Functions;
using Attempt17.Features.Primitives;
using Attempt17.Features.Variables;
using Attempt17.TypeChecking;

namespace Attempt17.Features {
    public class SyntaxCodeGenerator : ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> {
        public
        IContainersVisitor<CBlock, TypeCheckTag, CodeGenerationContext> ContainersVisitor { get; }
            = new ContainersCodeGenerator();

        public
        IPrimitivesVisitor<CBlock, TypeCheckTag, CodeGenerationContext> PrimitivesVisitor { get; }
            = new PrimitivesCodeGenerator();

        public
        IFunctionsVisitor<CBlock, TypeCheckTag, CodeGenerationContext> FunctionsVisitor { get; }
            = new FunctionsCodeGenerator();

        public
        IVariablesVisitor<CBlock, TypeCheckTag, CodeGenerationContext> VariablesVisitor { get; }
            = new VariablesCodeGenerator();

        public
        IFlowControlVisitor<CBlock, TypeCheckTag, CodeGenerationContext> FlowControlVisitor { get; }
            = new FlowControlCodeGenerator();
    }
}
