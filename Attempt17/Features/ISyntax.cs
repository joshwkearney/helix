namespace Attempt17.Features  {
    public interface ISyntax<TTag> {
        public TTag Tag { get; }

        public T Accept<T, TContext>(ISyntaxVisitor<T, TTag, TContext> visitor, TContext context);
    }
}
