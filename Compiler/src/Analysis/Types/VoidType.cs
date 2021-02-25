namespace Trophy.Analysis.Types {
    public class VoidType : TrophyType {
        public override bool IsVoidType => true;

        public VoidType() { }

        public override bool Equals(object other) => other is VoidType;

        public override int GetHashCode() => 5;

        public override string ToString() => "void";

        public override bool HasDefaultValue(ITypeRecorder types) => true;

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }
    }
}