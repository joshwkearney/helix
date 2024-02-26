using Helix.Analysis.Types;

namespace helix.common.Types {
    internal class TypeToStringVisitor : ITypeVisitor<string> {
        public static TypeToStringVisitor Instance { get; } = new();

        public string VisitArrayType(ArrayType type) {
            return type.InnerType + "[]";
        }

        public string VisitBoolType(BoolType type) {
            return "bool";
        }

        public string VisitFunctionType(FunctionType type) {
            var args = type.Parameters.Select(x => (x.IsMutable ? "var" : "let") + " " + x.Name + " as " + x.Type);

            return "func(" + string.Join(", ", type.Parameters) + ") as " + type.ReturnType;
        }

        public string VisitNominalType(NominalType type) {
            return type.Name;
        }

        public string VisitPointerType(PointerType type) {
            return type.InnerType.ToString() + "*";
        }

        public string VisitSingularBoolType(SingularBoolType type) {
            return type.Value ? "true" : "false";
        }

        public string VisitSingularWordType(SingularWordType type) {
            return type.Value.ToString();
        }

        public string VisitStructType(StructType type) {
            var members = type.Members.Select(x => (x.IsMutable ? "var" : "let") + " " + x.Name + " as " + x.Type);

            return "struct { " + string.Join("; ", members) + "}";
        }

        public string VisitUnionType(UnionType type) {
            var members = type.Members.Select(x => (x.IsMutable ? "var" : "let") + " " + x.Name + " as " + x.Type);

            return "union { " + string.Join("; ", members) + "}";
        }

        public string VisitVoidType(VoidType type) {
            return "void";
        }

        public string VisitWordType(WordType type) {
            return "word";
        }
    }
}
