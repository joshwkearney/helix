namespace Attempt16.Types {
    public interface ITypeVisitor<T> {
        T VisitIntType(IntType type);

        T VisitVariableType(VariableType type);

        T VisitVoidType(VoidType type);

        T VisitFunctionType(SingularFunctionType type);

        T VisitStructType(SingularStructType type);
    }
}