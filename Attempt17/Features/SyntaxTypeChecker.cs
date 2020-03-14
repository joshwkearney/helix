using System;
using Attempt17.Features.Containers;
using Attempt17.Features.FlowControl;
using Attempt17.Features.Functions;
using Attempt17.Features.Primitives;
using Attempt17.Features.Variables;
using Attempt17.Parsing;
using Attempt17.TypeChecking;

namespace Attempt17.Features  {
    public class SyntaxTypeChecker : ISyntaxVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> {
        public
        IContainersVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> ContainersVisitor
            { get; } = new ContainersTypeChecker();

        public
        IPrimitivesVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> PrimitivesVisitor
            { get; } = new PrimitivesTypeChecker();

        public
        IFunctionsVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> FunctionsVisitor
            { get; } = new FunctionsTypeChecker();

        public
        IVariablesVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> VariablesVisitor
            { get; } = new VariablesTypeChecker();

        public
        IFlowControlVisitor<ISyntax<TypeCheckTag>, ParseTag, TypeCheckContext> FlowControlVisitor
            { get; } = new FlowControlTypeChecker();
    }
}
