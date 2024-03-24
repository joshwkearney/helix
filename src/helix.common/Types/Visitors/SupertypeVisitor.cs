using Helix.Common.Types;

namespace Helix.Common.Types.Visitors {
    internal class SupertypeVisitor : ITypeVisitor<IHelixType> {
        public static SupertypeVisitor Instance { get; } = new();

        public IHelixType VisitArrayType(ArrayType type) => type;

        public IHelixType VisitBoolType(BoolType type) => type;

        public IHelixType VisitNominalType(NominalType type) => type;

        public IHelixType VisitPointerType(PointerType type) => type;

        public IHelixType VisitSingularBoolType(SingularBoolType type) => new BoolType();

        public IHelixType VisitSingularUnionType(SingularUnionType type) => type.UnionType;

        public IHelixType VisitSingularWordType(SingularWordType type) => new WordType();

        public IHelixType VisitVoidType(VoidType type) => type;

        public IHelixType VisitWordType(WordType type) => type;
    }
}
