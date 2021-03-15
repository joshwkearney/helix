using System.Diagnostics.CodeAnalysis;

namespace Trophy.Analysis.Types {
    public class IntType : ITrophyType {
        public bool IsIntType => true;

        public IntType() { }

        public override int GetHashCode() => 7;

        public override string ToString() => "int";

        public bool HasDefaultValue(ITypeRecorder types) => true;

        public TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }

        public override bool Equals(object other) => other is IntType;


        public bool Equals(ITrophyType other) {
            return other is IntType;
        }
    }
}