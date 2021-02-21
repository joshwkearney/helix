namespace Attempt20.Analysis.Types {
    public class FixedArrayType : TrophyType {
        public int Size { get; }

        public TrophyType ElementType { get; }

        public FixedArrayType(TrophyType elemType, int size) {
            this.ElementType = elemType;
            this.Size = size;
        }

        public override bool Equals(object obj) {
            return obj is FixedArrayType other && this.ElementType.Equals(other.ElementType) && this.Size == other.Size; 
        }

        public override TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Conditional;
        }

        public override int GetHashCode() {
            return this.Size + 7 * this.ElementType.GetHashCode();
        }

        public override bool HasDefaultValue(ITypeRecorder types) {
            return false;
        }

        public override string ToString() {
            return this.ElementType.ToString() + "[" + this.Size.ToString() + "]";
        }

        public override IOption<FixedArrayType> AsFixedArrayType() {
            return Option.Some(this);
        }
    }
}