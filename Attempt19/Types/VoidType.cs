using Attempt17.NewSyntax;

namespace Attempt18.Types {
    public class VoidType : LanguageType {
        public static VoidType Instance { get; } = new VoidType();

        private VoidType() { }

        public override bool Equals(object other) => other is VoidType;

        public override int GetHashCode() => 5;

        public override string ToString() => "void";

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitVoidType(this);
        }

        public override string ToFriendlyString() => "void";

        public override TypeCopiability GetCopiability() {
            return TypeCopiability.Unconditional;
        }
    }
}