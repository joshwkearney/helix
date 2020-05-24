namespace Attempt19.Types {
    public class ArrayType : LanguageType {
        public LanguageType ElementType { get; }

        public ArrayType(LanguageType elemType) {
            this.ElementType = elemType;
        }

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitArrayType(this);
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

        public override string ToFriendlyString() {
            return "array_" + this.ElementType.ToFriendlyString();
        }

        public override TypeCopiability GetCopiability() {
            return TypeCopiability.Conditional;
        }
    }
}