using Helix.Common.Types;
using Helix.Common.Types.Visitors;

namespace Helix.MiddleEnd.TypeVisitors
{
    internal class RecursiveFieldEnumerator : ITypeVisitor<IEnumerable<IHelixType>> {
        private readonly HashSet<IHelixType> visitedStructs = [];
        private readonly AnalysisContext context;

        public RecursiveFieldEnumerator(AnalysisContext context) {
            this.context = context;
        }

        public IEnumerable<IHelixType> VisitArrayType(ArrayType type) => [];

        public IEnumerable<IHelixType> VisitBoolType(BoolType type) => [];

        public IEnumerable<IHelixType> VisitNominalType(NominalType type) {
            if (this.context.Signatures.StructSignatures.TryGetValue(type, out var structSig)) {
                foreach (var mem in structSig.Members) {
                    foreach (var sub in mem.Type.Accept(this)) {
                        if (visitedStructs.Contains(sub)) {
                            continue;
                        }

                        visitedStructs.Add(sub);
                        yield return sub;
                    }
                }
            }
            else if (this.context.Signatures.UnionSignatures.TryGetValue(type, out var unionSig)) {
                foreach (var mem in unionSig.Members) {
                    foreach (var sub in mem.Type.Accept(this)) {
                        if (visitedStructs.Contains(sub)) {
                            continue;
                        }

                        visitedStructs.Add(sub);
                        yield return sub;
                    }
                }
            }
        }

        public IEnumerable<IHelixType> VisitPointerType(PointerType type) => [];

        public IEnumerable<IHelixType> VisitSingularBoolType(SingularBoolType type) => [];

        public IEnumerable<IHelixType> VisitSingularUnionType(SingularUnionType type) {
            return type.UnionType.Accept(this);
        }

        public IEnumerable<IHelixType> VisitSingularWordType(SingularWordType type) => [];

        public IEnumerable<IHelixType> VisitVoidType(VoidType type) => [];

        public IEnumerable<IHelixType> VisitWordType(WordType type) => [];
    }
}
