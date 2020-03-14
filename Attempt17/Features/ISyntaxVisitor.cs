using Attempt17.Features.Containers;
using Attempt17.Features.FlowControl;
using Attempt17.Features.Functions;
using Attempt17.Features.Primitives;
using Attempt17.Features.Variables;

namespace Attempt17.Features  {
    public interface ISyntaxVisitor<T, TTag, TContext> {
        public IContainersVisitor<T, TTag, TContext> ContainersVisitor { get; }

        public IPrimitivesVisitor<T, TTag, TContext> PrimitivesVisitor { get; }

        public IFunctionsVisitor<T, TTag, TContext> FunctionsVisitor { get; }

        public IVariablesVisitor<T, TTag, TContext> VariablesVisitor { get; }

        public IFlowControlVisitor<T, TTag, TContext> FlowControlVisitor { get; }
    }
}
