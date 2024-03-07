using Helix.Common;
using Helix.Common.Types;

namespace Helix.MiddleEnd.TypeChecking {
    internal class RecursiveFieldTypesEnumerator : ITypeVisitor<IEnumerable<IHelixType>> {
        private readonly HashSet<IHelixType> visitedStructs = [];
        private readonly TypeCheckingContext context;

        public RecursiveFieldTypesEnumerator(TypeCheckingContext context) {
            this.context = context;
        }

        public IEnumerable<IHelixType> VisitArrayType(ArrayType type) => [];

        public IEnumerable<IHelixType> VisitBoolType(BoolType type) => [];

        public IEnumerable<IHelixType> VisitFunctionType(FunctionType type) => [];

        public IEnumerable<IHelixType> VisitNominalType(NominalType type) {
            return this.context.Types.GetType(type.Name).Accept(this);
        }

        public IEnumerable<IHelixType> VisitPointerType(PointerType type) => [];

        public IEnumerable<IHelixType> VisitSingularBoolType(SingularBoolType type) => [];

        public IEnumerable<IHelixType> VisitSingularWordType(SingularWordType type) => [];

        public IEnumerable<IHelixType> VisitStructType(StructType type) {
            foreach (var mem in type.Members) {
                foreach (var sub in mem.Type.Accept(this)) {
                    if (this.visitedStructs.Contains(sub)) {
                        continue;
                    }

                    this.visitedStructs.Add(sub);
                    yield return sub;
                }
            }
        }

        public IEnumerable<IHelixType> VisitUnionType(UnionType type) {
            foreach (var mem in type.Members) {
                foreach (var sub in mem.Type.Accept(this)) {
                    if (this.visitedStructs.Contains(sub)) {
                        continue;
                    }

                    this.visitedStructs.Add(sub);
                    yield return sub;
                }
            }
        }

        public IEnumerable<IHelixType> VisitVoidType(VoidType type) => [];

        public IEnumerable<IHelixType> VisitWordType(WordType type) => [];
    }
}
