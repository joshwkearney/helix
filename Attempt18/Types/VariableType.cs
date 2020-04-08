using System.Collections.Immutable;

namespace Attempt18.Types {
    public class VariableType : LanguageType {
        public LanguageType InnerType { get; }

        public override LanguageTypeKind Kind => LanguageTypeKind.Variable;

        public VariableType(LanguageType innerType) {
            this.InnerType = innerType;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is VariableType varType) {
                return this.InnerType == varType.InnerType;
            }

            return false;
        }

        public override int GetHashCode() => 7 * this.InnerType.GetHashCode();

        public override string ToString() => "var " + this.InnerType.ToString();

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitVariableType(this);
        }
    }
}