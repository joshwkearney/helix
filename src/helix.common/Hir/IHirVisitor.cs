namespace Helix.Common.Hmm {
    public interface IHirVisitor<T> {
        public T VisitDereference(HirDereference syntax);

        public T VisitIndex(HirIndex syntax);

        public T VisitAddressOf(HirAddressOf syntax);

        public T VisitUnaryOperator(HirUnaryOperator syntax);

        public T VisitBinarySyntax(HirBinarySyntax syntax);

        public T VisitNew(HirNewSyntax syntax);

        public T VisitVariableStatement(HirVariableStatement syntax);

        public T VisitAssignment(HmmAssignment syntax);

        public T VisitIs(HirIsSyntax syntax);

        public T VisitMemberAccess(HirMemberAccess syntax);

        public T VisitFunctionDeclaration(HirFunctionDeclaration syntax);

        public T VisitInvoke(HirInvokeSyntax syntax);

        public T VisitReturn(HmmReturnSyntax syntax);

        public T VisitBreak(HmmBreakSyntax syntax);

        public T VisitContinue(HmmContinueSyntax syntax);

        public T VisitIfExpression(HirIfExpression syntax);

        public T VisitLoop(HirLoopSyntax syntax);

        public T VisitArrayLiteral(HirArrayLiteral syntax);

        public T VisitStructDeclaration(HmmStructDeclaration syntax);

        public T VisitUnionDeclaration(HmmUnionDeclaration syntax);

        public T VisitTypeDeclaration(HmmTypeDeclaration syntax);

        public T VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax);

        public T VisitIntrinsicUnionMemberAccess(HirIntrinsicUnionMemberAccess syntax);
    }
}