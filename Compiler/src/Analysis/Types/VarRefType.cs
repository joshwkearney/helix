namespace Trophy.Analysis.Types {
    public class VarRefType : TrophyType {
        public TrophyType InnerType { get; }

        public bool IsReadOnly { get; }

        public VarRefType(TrophyType innerType, bool isReadonly) {
            this.InnerType = innerType;
            this.IsReadOnly = isReadonly;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is VarRefType varType) {
                return this.InnerType == varType.InnerType && this.IsReadOnly == varType.IsReadOnly;
            }

            return false;
        }

        public override bool HasDefaultValue(ITypeRecorder types) => false;

        public override int GetHashCode() => this.IsReadOnly.GetHashCode() + 7 * this.InnerType.GetHashCode();

        public override string ToString() {
            if (this.IsReadOnly) {
                return "ref " + this.InnerType.ToString();
            }
            else {
                return "var " + this.InnerType.ToString();
            }
        }

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Conditional;
        }

        public override IOption<VarRefType> AsVariableType() {
            return Option.Some(this);
        }
    }
}