using Helix.Common.Types;

namespace Helix.MiddleEnd.TypeVisitors {
    internal class SupertypeVisitor : ITypeVisitor<IHelixType> {
        public static SupertypeVisitor Instance { get; } = new();

        public IHelixType VisitArrayType(ArrayType type) => type;

        public IHelixType VisitBoolType(BoolType type) => type;

        public IHelixType VisitFunctionType(FunctionType type) => type;

        public IHelixType VisitNominalType(NominalType type) => type;

        public IHelixType VisitPointerType(PointerType type) => type;

        public IHelixType VisitSingularBoolType(SingularBoolType type) => new BoolType();

        public IHelixType VisitSingularWordType(SingularWordType type) => new WordType();

        public IHelixType VisitStructType(StructType type) => type;

        public IHelixType VisitUnionType(UnionType type) => type;

        public IHelixType VisitVoidType(VoidType type) => type;

        public IHelixType VisitWordType(WordType type) => type;
    }
}
