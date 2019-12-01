namespace Attempt12.Analyzing {
    public interface ISyntaxVisitor {
        void Visit(Int32Syntax syntax);
        void Visit(Real32Syntax syntax);
        void Visit(VariableReferenceSyntax syntax);
        void Visit(VariableDeclarationSyntax syntax);
        void Visit(FunctionDefinitionSyntax syntax);
        void Visit(IntrinsicSyntax syntax);
        void Visit(FunctionInvokeSyntax syntax);
        void Visit(StatementSyntax syntax);
    }
}