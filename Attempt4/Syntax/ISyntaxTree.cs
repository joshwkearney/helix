namespace Attempt4 {
    public interface ISyntaxTree {
        void Accept(ISyntaxVisitor visitor);
    }
}