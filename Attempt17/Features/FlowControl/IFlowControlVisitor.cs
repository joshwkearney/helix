using System;
namespace Attempt17.Features.FlowControl {
    public interface IFlowControlVisitor<T, TTag, TContext> {
        public T VisitBlock(BlockSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor,
            TContext context);

        public T VisitIf(IfSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor,
            TContext context);

        public T VisitWhile(WhileSyntax<TTag> syntax, ISyntaxVisitor<T, TTag, TContext> visitor,
            TContext context);
    }
}
