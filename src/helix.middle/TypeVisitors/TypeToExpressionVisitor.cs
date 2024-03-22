using Helix.Common;
using Helix.Common.Types;

namespace Helix.MiddleEnd.TypeVisitors {
    internal class TypeToExpressionVisitor : ITypeVisitor<Option<string>> {
        public static TypeToExpressionVisitor Instance { get; } = new();

        public Option<string> VisitArrayType(ArrayType type) => Option.None;

        public Option<string> VisitBoolType(BoolType type) => Option.None;

        public Option<string> VisitFunctionType(FunctionType type) => Option.None;

        public Option<string> VisitNominalType(NominalType type) => Option.None;

        public Option<string> VisitPointerType(PointerType type) => Option.None;

        public Option<string> VisitSingularBoolType(SingularBoolType type) => type.ToString();

        public Option<string> VisitSingularUnionType(SingularUnionType type) => Option.None;

        public Option<string> VisitSingularWordType(SingularWordType type) => type.ToString();

        public Option<string> VisitStructType(StructType type) => Option.None;

        public Option<string> VisitUnionType(UnionType type) => Option.None;

        public Option<string> VisitVoidType(VoidType type) => "void";

        public Option<string> VisitWordType(WordType type) => Option.None;
    }
}
