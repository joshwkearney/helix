namespace Attempt19.Types {
    public interface ITypeVisitor<T> {
        T VisitIntType(IntType type);

        T VisitVoidType(VoidType type);

        T VisitBoolType(BoolType type);

        T VisitVariableType(VariableType type);

        T VisitArrayType(ArrayType type);
    }
}