namespace Attempt3 {
    public interface IValueVisitor {
        void Visit(IInterpretedValue value);
        void Visit(IntegerLiteral value);
        void Visit(FunctionCall value);
        void Visit(IntrinsicLiteral value);
    }
}