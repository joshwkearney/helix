using Helix.Common.Types;

namespace Helix.MiddleEnd.TypeVisitors {
    internal class RecursiveFieldEnumerator : ITypeVisitor<IEnumerable<IHelixType>> {
        private readonly HashSet<IHelixType> visitedStructs = [];
        private readonly AnalysisContext context;

        public RecursiveFieldEnumerator(AnalysisContext context) {
            this.context = context;
        }

        public IEnumerable<IHelixType> VisitArrayType(ArrayType type) => [];

        public IEnumerable<IHelixType> VisitBoolType(BoolType type) => [];

        public IEnumerable<IHelixType> VisitFunctionType(FunctionType type) => [];

        public IEnumerable<IHelixType> VisitNominalType(NominalType type) {
            return context.Types[type.Name].Accept(this);
        }

        public IEnumerable<IHelixType> VisitPointerType(PointerType type) => [];

        public IEnumerable<IHelixType> VisitSingularBoolType(SingularBoolType type) => [];

        public IEnumerable<IHelixType> VisitSingularUnionType(SingularUnionType type) {
            return type.Signature.Accept(this);
        }

        public IEnumerable<IHelixType> VisitSingularWordType(SingularWordType type) => [];

        public IEnumerable<IHelixType> VisitStructType(StructType type) {
            foreach (var mem in type.Members) {
                foreach (var sub in mem.Type.Accept(this)) {
                    if (visitedStructs.Contains(sub)) {
                        continue;
                    }

                    visitedStructs.Add(sub);
                    yield return sub;
                }
            }
        }

        public IEnumerable<IHelixType> VisitUnionType(UnionType type) {
            foreach (var mem in type.Members) {
                foreach (var sub in mem.Type.Accept(this)) {
                    if (visitedStructs.Contains(sub)) {
                        continue;
                    }

                    visitedStructs.Add(sub);
                    yield return sub;
                }
            }
        }

        public IEnumerable<IHelixType> VisitVoidType(VoidType type) => [];

        public IEnumerable<IHelixType> VisitWordType(WordType type) => [];
    }
}
