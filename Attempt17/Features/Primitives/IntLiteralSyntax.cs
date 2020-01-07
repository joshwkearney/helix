namespace Attempt17.Features.Primitives {
    public class IntLiteralSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public long Value { get; }

        public IntLiteralSyntax(T tag, long value) {
            this.Tag = tag;
            this.Value = value;
        }
    }
}