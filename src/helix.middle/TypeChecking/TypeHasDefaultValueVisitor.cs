using Helix.Common;
using Helix.Common.Types;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeHasDefaultValueVisitor : ITypeVisitor<bool> {
        private readonly TypeCheckingContext context;

        public TypeHasDefaultValueVisitor(TypeCheckingContext context) {
            this.context = context;
        }

        public bool VisitArrayType(ArrayType type) => true;

        public bool VisitBoolType(BoolType type) => true;

        public bool VisitFunctionType(FunctionType type) {
            throw new NotImplementedException();
        }

        public bool VisitNominalType(NominalType type) {
            return this.context.Types.GetType(type.Name).Accept(this);
        }

        public bool VisitPointerType(PointerType type) => false;

        public bool VisitSingularBoolType(SingularBoolType type) => type.Value == false;

        public bool VisitSingularWordType(SingularWordType type) => type.Value == 0;

        public bool VisitStructType(StructType type) => type.Members.All(x => x.Type.Accept(this));

        public bool VisitUnionType(UnionType type) => type.Members.Count == 0 || type.Members.First().Type.Accept(this);

        public bool VisitVoidType(VoidType type) => true;

        public bool VisitWordType(WordType type) => true;
    }
}
