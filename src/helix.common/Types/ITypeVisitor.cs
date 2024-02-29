namespace Helix.Common.Types {
    public interface ITypeVisitor<T> {
        public T VisitVoidType(VoidType type);

        public T VisitWordType(WordType type);

        public T VisitBoolType(BoolType type);

        public T VisitSingularWordType(SingularWordType type);

        public T VisitSingularBoolType(SingularBoolType type);

        public T VisitArrayType(ArrayType type);

        public T VisitFunctionType(FunctionType type);

        public T VisitNominalType(NominalType type);

        public T VisitPointerType(PointerType type);

        public T VisitStructType(StructType type);

        public T VisitUnionType(UnionType type);
    }
}