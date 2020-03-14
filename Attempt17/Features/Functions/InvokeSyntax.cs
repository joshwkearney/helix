using Attempt17.Parsing;
using System.Collections.Immutable;

namespace Attempt17.Features.Functions {
    public class InvokeSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Target { get; }

        public ImmutableList<ISyntax<T>> Arguments { get; }

        public InvokeSyntax(T tag, ISyntax<T> target, ImmutableList<ISyntax<T>> arguments) {
            this.Tag = tag;
            this.Target = target;
            this.Arguments = arguments;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.FunctionsVisitor.VisitInvoke(this, visitor, context);
        }
    }
}