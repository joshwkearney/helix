namespace Attempt12 {
    public enum PrimitiveTrophyTypes {
        Int64, Boolean, Real64
    }

    public class PrimitiveTrophyType : ITrophyType {
        public static ITrophyType Int64Type { get; } = new PrimitiveTrophyType(PrimitiveTrophyTypes.Int64);
        public static ITrophyType Real64Type { get; } = new PrimitiveTrophyType(PrimitiveTrophyTypes.Real64);
        public static ITrophyType Boolean { get; } = new PrimitiveTrophyType(PrimitiveTrophyTypes.Boolean);

        public PrimitiveTrophyTypes Kind { get; }

        public bool IsLiteral => true;

        public bool IsMovable => true;

        private PrimitiveTrophyType(PrimitiveTrophyTypes kind) {
            this.Kind = kind;
        }

        public void Accept(ITrophyTypeVisitor visitor) => visitor.Visit(this);

        public bool Equals(ITrophyType other) {
            if (!(other is PrimitiveTrophyType primitiveType)) {
                return false;
            }

            if (this.Kind != primitiveType.Kind) {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj) {
            if (obj is ITrophyType type) {
                return this.Equals(type);
            }

            return false;
        }

        public override int GetHashCode() {
            var hashCode = -1063252000;
            hashCode = hashCode * -1521134295 + this.Kind.GetHashCode();
            return hashCode;
        }

        public bool IsCompatibleWith(ITrophyType other) => this.Equals(other);
    }
}