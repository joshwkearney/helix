namespace Attempt17.Features.Variables {
    public class StoreSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public ISyntax<T> Target { get; }

        public ISyntax<T> Value { get; }

        public StoreSyntax(T tag, ISyntax<T> target, ISyntax<T> value) {
            this.Tag = tag;
            this.Target = target;
            this.Value = value;
        }
    }
}
