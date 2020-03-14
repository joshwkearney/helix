namespace Attempt17 {
    public interface IIdentifierTargetVisitor<T> {
        public T VisitVariable(VariableInfo variable);

        public T VisitFunction(FunctionInfo function);

        public T VisitComposite(CompositeInfo composite);

        public T VisitReserved(ReservedIdentifier reserved);
    }
}