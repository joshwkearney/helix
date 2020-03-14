namespace Attempt17.Features.Primitives {
    public enum BinarySyntaxKind {
        Add, Subtract, Multiply, 
        And, Or, Xor,
        EqualTo, NotEqualTo,
        GreaterThan, LessThan,
        GreaterThanOrEqualTo, LessThanOrEqualTo
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

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor.PrimitivesVisitor.VisitBinary(this, visitor, context);
        }
    }
}