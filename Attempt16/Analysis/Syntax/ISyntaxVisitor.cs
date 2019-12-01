namespace Attempt16.Syntax {
    public interface ISyntaxVisitor<T> {
        T VisitBlock(BlockSyntax syntax);

        T VisitVariableInitialization(VariableStatement syntax);

        T VisitVariableLiteral(VariableLiteral syntax);

        T VisitIntLiteral(IntLiteral syntax);

        T VisitVariableLocationLiteral(VariableLocationLiteral syntax);

        T VisitStore(StoreSyntax syntax);

        T VisitIf(IfSyntax syntax);

        T VisitValueof(ValueofSyntax syntax);

        T VisitBinaryExpression(BinaryExpression syntax);

        T VisitWhileStatement(WhileStatement syntax);

        T VisitFunctionCall(FunctionCallSyntax syntax);

        T VisitStructInitialization(StructInitializationSyntax syntax);

        T VisitMemberAccessSyntax(MemberAccessSyntax syntax);
    }
}
