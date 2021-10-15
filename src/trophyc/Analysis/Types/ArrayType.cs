using System.Diagnostics.CodeAnalysis;

namespace Trophy.Analysis.Types {
    public class ArrayType : ITrophyType {
        public bool IsReadOnly { get; }

        public ITrophyType ElementType { get; }

        public ArrayType(ITrophyType elemType, bool isReadOnly) {
            this.ElementType = elemType;
            this.IsReadOnly = isReadOnly;
        }

        public override int GetHashCode() {
            return this.IsReadOnly.GetHashCode() + 3 * ElementType.GetHashCode();
        }

        public override string ToString() {
            if (this.IsReadOnly) {
                return "span[" + this.ElementType + "]";
            }
            else {
                return "array[" + this.ElementType + "]";
            }
        }

        public bool HasDefaultValue(ITypesRecorder types) => true;

        public TypeCopiability GetCopiability(ITypesRecorder types) => TypeCopiability.Conditional;

        public IOption<ArrayType> AsArrayType() => Option.Some(this);

        public override bool Equals([AllowNull] object other) {
            return this.Equals(other as ITrophyType);
        }

        public bool Equals([AllowNull] ITrophyType other) {
            if (other is null) {
                return false;
            }

            if (other is ArrayType arrType) {
                return this.ElementType.Equals(arrType.ElementType) && this.IsReadOnly == arrType.IsReadOnly;
            }

            return false;
        }
    }
}