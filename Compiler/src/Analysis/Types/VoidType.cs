using System.Diagnostics.CodeAnalysis;

namespace Trophy.Analysis.Types {
    public class VoidType : ITrophyType {
        public bool IsVoidType => true;

        public VoidType() { }

        public override bool Equals(object other) => other is VoidType;

        public override int GetHashCode() => 5;

        public override string ToString() => "void";

        public bool HasDefaultValue(ITypeRecorder types) => true;

        public TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }

        public bool Equals(ITrophyType other) => other is VoidType;
    }
}