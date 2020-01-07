using System.Collections.Immutable;

namespace Attempt17.Features.FlowControl {
    public class BlockSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ImmutableList<ISyntax<T>> Statements { get; }

        public BlockSyntax(T tag, ImmutableList<ISyntax<T>> stats) {
            this.Tag = tag;
            this.Statements = stats;
        }
    }
}