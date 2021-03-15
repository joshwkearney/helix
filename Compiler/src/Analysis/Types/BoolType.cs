using System.Diagnostics.CodeAnalysis;

namespace Trophy.Analysis.Types {
    public class BoolType : ITrophyType {
        public bool IsBoolType => true;

        public BoolType() { }

        public override int GetHashCode() => 11;

        public override string ToString() => "bool";

        public bool HasDefaultValue(ITypeRecorder types) => true;

        public TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }

        public override bool Equals([AllowNull] object other) => other is BoolType;

        public bool Equals([AllowNull] ITrophyType other) => other is BoolType;
    }
}