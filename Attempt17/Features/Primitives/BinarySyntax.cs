namespace Attempt17.Features.Primitives {
    public enum BinarySyntaxKind {
        Add, Subtract, Multiply
    }

    public class BinarySyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public BinarySyntaxKind Kind { get; }

        public ISyntax<T> Left { get; }

        public ISyntax<T> Right { get; }

        public BinarySyntax(T tag, BinarySyntaxKind kind, ISyntax<T> left, ISyntax<T> right) {
            this.Tag = tag;
            this.Kind = kind;
            this.Left = left;
            this.Right = right;
        }
    }
}