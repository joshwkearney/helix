namespace Attempt4 {
    public interface IAnalyzedSyntaxVisitor {
        void Visit(FunctionCall expr);
        void Visit(IInterpretedValue value);
        void Visit(IntrinsicFunction func);
    }
}