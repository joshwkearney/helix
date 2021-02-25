namespace Trophy.Analysis.Types {
    public class FixedArrayType : TrophyType {
        public int Size { get; }

        public TrophyType ElementType { get; }

        public bool IsReadOnly { get; }

        public FixedArrayType(TrophyType elemType, int size, bool isReadOnly) {
            this.ElementType = elemType;
            this.Size = size;
            this.IsReadOnly = isReadOnly;
        }

        public override bool Equals(object obj) {
            return obj is FixedArrayType other 
                && this.ElementType.Equals(other.ElementType) 
                && this.Size == other.Size
                && this.IsReadOnly == other.IsReadOnly; 
        }

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Conditional;
        }

        public override int GetHashCode() {
            return this.IsReadOnly.GetHashCode() 
                + 3 * this.Size 
                + 7 * this.ElementType.GetHashCode();
        }

        public override bool HasDefaultValue(ITypeRecorder types) {
            return false;
        }

        public override string ToString() {
            return "array[" + (this.IsReadOnly ? "ref " : "var ") + this.ElementType + ", " + this.Size + "]";
        }

        public override IOption<FixedArrayType> AsFixedArrayType() {
            return Option.Some(this);
        }

        public override IOption<ArrayType> AsArrayType() {
            return Option.Some(new ArrayType(this.ElementType, this.IsReadOnly));
        }
    }
}