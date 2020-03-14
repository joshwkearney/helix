namespace Attempt17.Features.FlowControl {
    public class WhileSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Condition { get; }

        public ISyntax<T> Body { get; }

        public WhileSyntax(T tag, ISyntax<T> cond, ISyntax<T> body) {
            this.Tag = tag;
            this.Condition = cond;
            this.Body = body;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.FlowControlVisitor.VisitWhile(this, visitor, context);
        }
    }
}