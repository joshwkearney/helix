namespace Attempt9 {
    public interface IStatementVisitor {
        void Visit(IfStatement stat);
        void Visit(VariableDeclarationSyntax stat);
        void Visit(VariableAssignmentSyntax stat);
        void Visit(ReferenceCheckoutStatement stat);
        void Visit(ReferenceCheckinStatement stat);
        void Visit(ReferenceCreateStatement stat);
        void Visit(VariableDeclarationAssignmentSyntax stat);
    }
}