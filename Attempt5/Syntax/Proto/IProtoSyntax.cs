namespace Attempt6.Syntax {
    public interface IProtoSyntax {
        void Accept(IProtoSyntaxVisitor visitor);
    }
}