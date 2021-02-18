namespace Attempt20.Analysis.Types {
    public class BoolType : TrophyType {
        public override bool IsBoolType => true;

        public BoolType() { }

        public override bool Equals(object other) => other is BoolType;

        public override int GetHashCode() => 11;

        public override string ToString() => "bool";

        public override bool HasDefaultValue(ITypeRecorder types) => true;

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Unconditional;
        }
    }
}