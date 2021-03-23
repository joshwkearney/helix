using System.Diagnostics.CodeAnalysis;

namespace Trophy.Analysis.Types {
    public class FixedArrayType : ITrophyType {
        public int Size { get; }

        public ITrophyType ElementType { get; }

        public bool IsReadOnly { get; }

        public FixedArrayType(ITrophyType elemType, int size, bool isReadOnly) {
            this.ElementType = elemType;
            this.Size = size;
            this.IsReadOnly = isReadOnly;
        }

        public TypeCopiability GetCopiability(ITypesRecorder types) {
            return TypeCopiability.Conditional;
        }

        public override int GetHashCode() {
            return this.IsReadOnly.GetHashCode() 
                + 3 * this.Size 
                + 7 * this.ElementType.GetHashCode();
        }

        public bool HasDefaultValue(ITypesRecorder types) {
            return false;
        }

        public override string ToString() {
            return "array[" + (this.IsReadOnly ? "ref " : "var ") + this.ElementType + ", " + this.Size + "]";
        }

        public IOption<FixedArrayType> AsFixedArrayType() {
            return Option.Some(this);
        }

        public IOption<ArrayType> AsArrayType() {
            return Option.Some(new ArrayType(this.ElementType, this.IsReadOnly));
        }

        public override bool Equals([AllowNull] object obj) {
            return this.Equals(obj as ITrophyType);
        }

        public bool Equals([AllowNull] ITrophyType obj) {
            return obj is FixedArrayType other
                && this.ElementType.Equals(other.ElementType)
                && this.Size == other.Size
                && this.IsReadOnly == other.IsReadOnly;
        }
    }
}