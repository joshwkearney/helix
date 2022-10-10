using System.Diagnostics.CodeAnalysis;

namespace Trophy.Parsing {
    public class VariableType : ITrophyType {
        public ITrophyType InnerType { get; }

        public bool IsReadOnly { get; }

        public VariableType(ITrophyType innerType, bool isReadonly) {
            this.InnerType = innerType;
            this.IsReadOnly = isReadonly;
        }

        public bool HasDefaultValue(NameTable types) => false;

        public override int GetHashCode() => this.IsReadOnly.GetHashCode() + 7 * this.InnerType.GetHashCode();

        public override string ToString() {
            if (this.IsReadOnly) {
                return "ref[" + this.InnerType + "]";
            }
            else {
                return "var[" + this.InnerType + "]";
            }
        }

        public bool TryGetVariableType(out VariableType type) {
            type = this;
            return true;
        }

        public override bool Equals(object obj) {
            return this.Equals(obj as ITrophyType);
        }

        public bool Equals(ITrophyType other) {
            if (other is null) {
                return false;
            }

            if (other is VariableType varType) {
                return this.InnerType.Equals(varType.InnerType) && this.IsReadOnly == varType.IsReadOnly;
            }

            return false;
        }
    }
}