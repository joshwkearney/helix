namespace Helix.Common.Types.Visitors {
    public interface ITypeVisitor<T> {
        public T VisitVoidType(VoidType type);

        public T VisitWordType(WordType type);

        public T VisitBoolType(BoolType type);

        public T VisitSingularWordType(SingularWordType type);

        public T VisitSingularBoolType(SingularBoolType type);

        public T VisitArrayType(ArrayType type);

        public T VisitNominalType(NominalType type);

        public T VisitPointerType(PointerType type);

        public T VisitSingularUnionType(SingularUnionType type);
    }
}