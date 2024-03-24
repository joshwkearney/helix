namespace Helix.Common.Types.Visitors {
    internal class TypeStringifier : ITypeVisitor<string> {
        public static TypeStringifier Instance { get; } = new();

        public string VisitArrayType(ArrayType type) {
            return type.InnerType + "[]";
        }

        public string VisitBoolType(BoolType type) {
            return "bool";
        }

        public string VisitFunctionType(FunctionSignature type) {
            var args = type.Parameters.Select(x => (x.IsMutable ? "var" : "let") + " " + x.Name + " as " + x.Type);

            return "func(" + string.Join(", ", type.Parameters) + ") as " + type.ReturnType;
        }

        public string VisitNominalType(NominalType type) {
            return type.DisplayName;
        }

        public string VisitPointerType(PointerType type) {
            return "*" + type.InnerType.ToString();
        }

        public string VisitSingularBoolType(SingularBoolType type) {
            if (type.Predicate.IsTrue) {
                return "true";
            }
            else if (type.Predicate.IsFalse) {
                return "false";
            }
            else {
                return $"bool[{type.Predicate}]";
            }
        }

        public string VisitSingularStructType(SingularStructType type) {
            var mems = type.Members.OrderBy(x => x.Name).Select(x => x.Name + " as " + x.Type);

            return type.StructType + " { " + string.Join("; ", mems) + " }";
        }

        public string VisitSingularUnionType(SingularUnionType type) {
            return type.UnionType + " { " + type.Member + " as " + type.Value + "; }";
        }

        public string VisitSingularWordType(SingularWordType type) {
            return type.Value.ToString();
        }

        public string VisitStructType(StructSignature type) {
            var members = type.Members.Select(x => (x.IsMutable ? "var" : "let") + " " + x.Name + " as " + x.Type + "; ");

            return "struct { " + string.Join("", members) + "}";
        }

        public string VisitUnionType(UnionSignature type) {
            var members = type.Members.Select(x => (x.IsMutable ? "var" : "let") + " " + x.Name + " as " + x.Type + "; ");

            return "union { " + string.Join("", members) + "}";
        }

        public string VisitVoidType(VoidType type) {
            return "void";
        }

        public string VisitWordType(WordType type) {
            return "word";
        }
    }
}
