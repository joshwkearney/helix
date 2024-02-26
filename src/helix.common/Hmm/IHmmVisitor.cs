namespace Helix.HelixMinusMinus {
    public interface IHmmVisitor {
        public void VisitWordLiteral(HmmWordLiteral syntax);

        public void VisitBoolLiteral(HmmBoolLiteral syntax);

        public void VisitVoidLiteral(HmmVoidLiteral syntax);

        public void VisitUnaryOperator(HmmUnaryOperator syntax);

        public void VisitBinaryOperator(HmmBinaryOperator syntax);

        public void VisitNew(HmmNewSyntax syntax);

        public void VisitAsSyntax(HmmAsSyntax syntax);

        public void VisitVariableStatement(HmmVariableStatement syntax);

        public void VisitVariableAccess(HmmVariableAccess syntax);

        public void VisitAssignment(HmmAssignment syntax);

        public void VisitIs(HmmIsSyntax syntax);

        public void VisitMemberAccess(HmmMemberAccess syntax);

        public void VisitFunctionDeclaration(HmmFunctionDeclaration syntax);

        public void VisitInvoke(HmmInvokeSyntax syntax);

        public void VisitReturn(HmmReturnSyntax syntax);

        public void VisitBreak(HmmBreakSyntax syntax);

        public void VisitContinue(HmmContinueSyntax syntax);

        public void VisitIfExpression(HmmIfExpression syntax);

        public void VisitLoop(HmmLoopSyntax syntax);

        public void VisitArrayLiteral(HmmArrayLiteral syntax);

        public void VisitStructDeclaration(HmmStructDeclaration syntax);

        public void VisitUnionDeclaration(HmmUnionDeclaration syntax);

        public void VisitTypeDeclaration(HmmTypeDeclaration syntax);

        public void VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax);
    }
}