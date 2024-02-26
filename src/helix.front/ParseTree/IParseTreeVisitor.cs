namespace helix_frontend.ParseTree {
    public interface IParseTreeVisitor<T> {
        public T VisitAssignment(AssignmentStatement syntax);

        public T VisitVariableAccess(VariableAccess syntax);

        public T VisitVariableStatement(VariableStatement syntax);

        public T VisitFunctionDeclaration(FunctionDeclaration syntax);

        public T VisitStructDeclaration(StructDeclaration syntax);

        public T VisitUnionDeclaration(UnionDeclaration syntax);

        public T VisitBinarySyntax(BinarySyntax syntax);

        public T VisitBoolLiteral(BoolLiteral syntax);

        public T VisitIf(IfSyntax syntax);

        public T VisitUnarySyntax(UnarySyntax syntax);

        public T VisitAs(AsSyntax syntax);

        public T VisitIs(IsSyntax syntax);

        public T VisitInvoke(InvokeSyntax syntax);

        public T VisitMemberAccess(MemberAccessSyntax syntax);

        public T VisitWordLiteral(WordLiteral syntax);

        public T VisitVoidLiteral(VoidLiteral syntax);

        public T VisitBlock(BlockSyntax syntax);

        public T VisitNew(NewSyntax syntax);

        public T VisitArrayLiteral(ArrayLiteral syntax);

        public T VisitBreak(BreakSyntax syntax);

        public T VisitContinue(ContinueSyntax syntax);

        public T VisitReturn(ReturnSyntax syntax);

        public T VisitWhile(WhileSyntax syntax);

        public T VisitFor(ForSyntax syntax);

        public T VisitLoop(LoopSyntax syntax);
    }
}
