namespace Attempt16.Syntax {
    public interface IDeclaration {
        string Name { get; set; }

        T Accept<T>(IDeclarationVisitor<T> visitor);
    }
}