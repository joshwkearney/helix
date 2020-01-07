namespace Attempt17.Features.Primitives {
    public class VoidLiteralSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public VoidLiteralSyntax(T tag) {
            this.Tag = tag;
        }
    }
}