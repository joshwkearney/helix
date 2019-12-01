namespace Attempt16.Syntax {
    public interface IDeclarationVisitor<T> {
        T VisitFunctionDeclaration(FunctionDeclaration decl);

        T VisitStructDeclaration(StructDeclaration decl);
    }
}