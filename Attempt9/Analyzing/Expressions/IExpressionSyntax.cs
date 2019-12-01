namespace Attempt9 {
    public interface IExpressionSyntax {
        ITrophyType ReturnType { get; }

        void Accept(IExpressionVisitor visitor);
    }
}