namespace Attempt20.Analysis.Types {
    public class ArrayType : TrophyType {
        public TrophyType ElementType { get; }

        public ArrayType(TrophyType elemType) {
            this.ElementType = elemType;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is ArrayType arrType) {
                return this.ElementType == arrType.ElementType;
            }

            return false;
        }

        public override int GetHashCode() {
            return ElementType.GetHashCode();
        }

        public override string ToString() {
            return this.ElementType.ToString() + "[]";
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