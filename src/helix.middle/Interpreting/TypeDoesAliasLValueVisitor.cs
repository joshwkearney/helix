using Helix.Common.Types;
using Helix.MiddleEnd.TypeChecking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.Interpreting {
    internal class TypeDoesAliasLValueVisitor : ITypeVisitor<bool> {
        public static TypeDoesAliasLValueVisitor Instance { get; } = new();

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
