//using Helix.Analysis.Types;
//using Helix.Frontend.NameResolution;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Helix.Frontend.CircularTypeChecker {
//    internal class ValueTypeEnumerator : ITypeVisitor<IEnumerable<IHelixType>> {
//        private readonly HashSet<IHelixType> visitedStructs = [];
//        private readonly DeclarationStore declarations;
//        private readonly IdentifierPath scope;

//        public ValueTypeEnumerator(IdentifierPath scope, DeclarationStore declarations) {
//            this.scope = scope;
//            this.declarations = declarations;
//        }

//        public IEnumerable<IHelixType> VisitArrayType(ArrayType type) => [];

//        public IEnumerable<IHelixType> VisitBoolType(BoolType type) => [];

//        public IEnumerable<IHelixType> VisitFunctionType(FunctionType type) => [];

//        public IEnumerable<IHelixType> VisitNominalType(NominalType type) {
//            return this.declarations.ResolveSignature(this.scope, type.Name)
//        }

//        public IEnumerable<IHelixType> VisitPointerType(PointerType type) => [];

//        public IEnumerable<IHelixType> VisitSingularBoolType(SingularBoolType type) => [];

//        public IEnumerable<IHelixType> VisitSingularWordType(SingularWordType type) => [];

//        public IEnumerable<IHelixType> VisitStructType(StructType type) {
//            foreach (var mem in type.Members) {
//                foreach (var sub in mem.Type.Accept(this)) {
//                    if (this.visitedStructs.Contains(sub)) {
//                        continue;
//                    }

//                    this.visitedStructs.Add(sub);
//                    yield return sub;
//                }
//            }
//        }

//        public IEnumerable<IHelixType> VisitUnionType(UnionType type) {
//            foreach (var mem in type.Members) {
//                foreach (var sub in mem.Type.Accept(this)) {
//                    if (this.visitedStructs.Contains(sub)) {
//                        continue;
//                    }

//                    this.visitedStructs.Add(sub);
//                    yield return sub;
//                }
//            }
//        }

//        public IEnumerable<IHelixType> VisitVoidType(VoidType type) => [];

//        public IEnumerable<IHelixType> VisitWordType(WordType type) => [];
//    }
//}
