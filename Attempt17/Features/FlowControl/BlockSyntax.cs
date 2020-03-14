using System.Collections.Immutable;

namespace Attempt17.Features.FlowControl {
    public class BlockSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ImmutableList<ISyntax<T>> Statements { get; }

        public BlockSyntax(T tag, ImmutableList<ISyntax<T>> stats) {
            this.Tag = tag;
            this.Statements = stats;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.FlowControlVisitor.VisitBlock(this, visitor, context);
        }
    }
}