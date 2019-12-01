using Attempt16.Analysis;

namespace Attempt16.Types {
    public class IntType : ILanguageType {
        public T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitIntType(this);
        }

        public override bool Equals(object other) {
            if (other == null) {
                return false;
            }

            if (other is IntType) {
                return true;
            }

            return false;
        }

        public override int GetHashCode() {
            return 7;
        }
    }
}