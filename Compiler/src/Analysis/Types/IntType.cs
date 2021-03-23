using System.Diagnostics.CodeAnalysis;

namespace Trophy.Analysis.Types {
    public class IntType : ITrophyType {
        public bool IsIntType => true;

        public IntType() { }

        public override int GetHashCode() => 7;

        public override string ToString() => "int";

        public bool HasDefaultValue(ITypesRecorder types) => true;

        public TypeCopiability GetCopiability(ITypesRecorder types) {
            return TypeCopiability.Unconditional;
        }

        public override bool Equals(object other) => other is IntType;


        public bool Equals(ITrophyType other) => other is IntType;
    }
}