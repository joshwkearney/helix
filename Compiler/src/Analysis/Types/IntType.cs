namespace Trophy.Analysis.Types {
    public class IntType : TrophyType {
        public override bool IsIntType => true;

        public IntType() { }

        public override bool Equals(object other) => other is IntType;

        public override int GetHashCode() => 7;

        public override string ToString() => "int";

        public override bool HasDefaultValue(ITypeRecorder types) => true;

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }
    }
}