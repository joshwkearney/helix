namespace Attempt9 {
    public interface IExpressionVisitor {
        void Visit(BinaryExpressionSyntax expr);
        void Visit(UnaryExpressionSyntax expr);
        void Visit(Int64LiteralSyntax expr);
        void Visit(VariableLiteralSyntax expr);
    }
}