namespace Trophy.Analysis.Types {
    public class ArrayType : TrophyType {
        public bool IsReadOnly { get; }

        public TrophyType ElementType { get; }

        public ArrayType(TrophyType elemType, bool isReadOnly) {
            this.ElementType = elemType;
            this.IsReadOnly = isReadOnly;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is ArrayType arrType) {
                return this.ElementType == arrType.ElementType && this.IsReadOnly == arrType.IsReadOnly;
            }

            return false;
        }

        public override int GetHashCode() {
            return this.IsReadOnly.GetHashCode() + ElementType.GetHashCode();
        }

        public override string ToString() {
            return "array[" + (this.IsReadOnly ? "ref " : "var ") + this.ElementType.ToString() + "]";
        }

        public override bool HasDefaultValue(ITypeRecorder types) => true;

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Conditional;
        }

        public override IOption<ArrayType> AsArrayType() {
            return Option.Some(this);
        }
    }
}