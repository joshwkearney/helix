using Attempt16.Analysis;

namespace Attempt16.Types {
    public class VoidType : ILanguageType {
        public T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitVoidType(this);
        }

        public override bool Equals(object obj) {
            if (obj == null) {
                return false;
            }

            if (obj is VoidType) {
                return true;
            }

            return false;
        }

        public override int GetHashCode() {
            return 1;
        }
    }
}