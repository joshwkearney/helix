using System.Collections.Immutable;

namespace Attempt19.Types {
    public class VoidType : LanguageType {
        public static VoidType Instance { get; } = new VoidType();

        public override LanguageTypeKind Kind => LanguageTypeKind.Void;

        private VoidType() { }

        public override bool Equals(object other) => other is VoidType;

        public override int GetHashCode() => 5;

        public override string ToString() => "void";

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitVoidType(this);
        }
    }
}