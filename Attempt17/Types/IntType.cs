namespace Attempt17.Types {
    public class IntType : LanguageType {
        public static IntType Instance { get; } = new IntType();

        private IntType() { }

        public override bool Equals(object other) => other is IntType;

        public override int GetHashCode() => 7;

        public override string ToString() => "int";

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitIntType(this);
        }
    }
}