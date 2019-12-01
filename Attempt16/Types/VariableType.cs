using Attempt16.Analysis;

namespace Attempt16.Types {
    public class VariableType : ILanguageType {
        public ILanguageType TargetType { get; }

        public VariableType(ILanguageType targetType) {
            this.TargetType = targetType;
        }

        public T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitVariableType(this);
        }

        public override bool Equals(object obj) {
            if (obj == null) {
                return false;
            }

            if (obj is VariableType varType) {
                return varType.TargetType.Equals(this.TargetType);
            }

            return false;
        }

        public override int GetHashCode() {
            return 101 + 3 * this.TargetType.GetHashCode();
        }
    }
}