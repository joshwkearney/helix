namespace Attempt9 {
    public interface IStatementSyntax {
        void Accept(IStatementVisitor visitor);
    }
}