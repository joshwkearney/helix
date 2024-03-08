using Helix.Common.Types;

namespace Helix.MiddleEnd.TypeVisitors {
    internal class DoesAliasLValueVisitor : ITypeVisitor<bool> {
        public static DoesAliasLValueVisitor Instance { get; } = new();

        public bool VisitArrayType(ArrayType type) => true;

        public bool VisitBoolType(BoolType type) => false;

        public bool VisitFunctionType(FunctionType type) => false;

        public bool VisitNominalType(NominalType type) => false;

        public bool VisitPointerType(PointerType type) => true;

        public bool VisitSingularBoolType(SingularBoolType type) => false;

        public bool VisitSingularWordType(SingularWordType type) => false;

        public bool VisitStructType(StructType type) => false;

        public bool VisitUnionType(UnionType type) => false;

        public bool VisitVoidType(VoidType type) => false;

        public bool VisitWordType(WordType type) => false;
    }
}
