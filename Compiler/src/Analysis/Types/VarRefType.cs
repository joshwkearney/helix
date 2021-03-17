using System.Diagnostics.CodeAnalysis;

namespace Trophy.Analysis.Types {
    public class VarRefType : ITrophyType {
        public ITrophyType InnerType { get; }

        public bool IsReadOnly { get; }

        public VarRefType(ITrophyType innerType, bool isReadonly) {
            this.InnerType = innerType;
            this.IsReadOnly = isReadonly;
        }


        public bool HasDefaultValue(ITypeRecorder types) => false;

        public override int GetHashCode() => this.IsReadOnly.GetHashCode() + 7 * this.InnerType.GetHashCode();

        public override string ToString() {
            if (this.IsReadOnly) {
                return "ref[" + this.InnerType + "]";
            }
            else {
                return "var[" + this.InnerType + "]";
            }
        }

        public TypeCopiability GetCopiability(ITypeRecorder types) {
            return TypeCopiability.Conditional;
        }

        public IOption<VarRefType> AsVariableType() {
            return Option.Some(this);
        }

        public override bool Equals(object obj) {
            return this.Equals(obj as ITrophyType);
        }

        public bool Equals(ITrophyType other) {
            if (other is null) {
                return false;
            }

            if (other is VarRefType varType) {
                return this.InnerType.Equals(varType.InnerType) && this.IsReadOnly == varType.IsReadOnly;
            }

            return false;
        }
    }
}