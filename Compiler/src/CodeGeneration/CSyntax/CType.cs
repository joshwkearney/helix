namespace Trophy.CodeGeneration.CSyntax {
    public abstract class CType {
        public static CType Integer { get; } = new CPrimitiveType("int");

        public static CType VoidPointer { get; } = Pointer(NamedType("void"));

        public static CType Pointer(CType inner) {
            return new CPointerType(inner);
        }

        public static CType NamedType(string name) {
            return new CPrimitiveType(name);
        }

        private CType() { }

        private class CPrimitiveType : CType {
            private readonly string name;

            public CPrimitiveType(string name) {
                this.name = name;
            }

            public override string ToString() {
                return this.name;
            }
        }

        private class CPointerType : CType {
            private readonly CType innerType;

            public CPointerType(CType inner) {
                this.innerType = inner;
            }

            public override string ToString() {
                return this.innerType.ToString() + "*";
            }
        }
    }
}
