namespace Attempt4 {
    public interface ISyntaxVisitor {
        void Visit(IntegerLiteral literal);
        void Visit(IdentifierLiteral literal);
        void Visit(FunctionCallExpression expr);
    }
}