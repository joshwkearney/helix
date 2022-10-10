using System;

namespace Trophy.Parsing {
    public interface ITrophyType : IEquatable<ITrophyType> {
        public static ITrophyType Boolean { get; } = new BoolType();

        public static ITrophyType Integer { get; } = new IntType();

        public static ITrophyType Void { get; } = new VoidType();

        public bool HasDefaultValue(NameTable types);

        public bool IsBoolType => false;

        public bool IsIntType => false;

        public bool IsVoidType => false;

        public bool TryGetArrayType(out ArrayType type) {
            type = null;
            return false;
        }

        public bool TryGetVariableType(out VariableType type) {
            type = null;
            return false;
        } 

        public bool AsNamedType(out NamedType type) {
            type = null;
            return false;
        }

        public bool AsFunctionType(out FunctionType type) {
            type = null;
            return false;
        }
    }
}