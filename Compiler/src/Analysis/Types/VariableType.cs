namespace Attempt20.Analysis.Types {
    public class VariableType : TrophyType {
        public TrophyType InnerType { get; }

        public VariableType(TrophyType innerType) {
            this.InnerType = innerType;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is VariableType varType) {
                return this.InnerType == varType.InnerType;
            }

            return false;
        }

        public override bool HasDefaultValue(ITypeRecorder types) => false;

        public override int GetHashCode() => 7 * this.InnerType.GetHashCode();

        public override string ToString() => "var " + this.InnerType.ToString();

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Conditional;
        }

        public override IOption<VariableType> AsVariableType() {
            return Option.Some(this);
        }
    }
}