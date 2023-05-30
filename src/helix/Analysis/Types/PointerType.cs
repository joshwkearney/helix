using Helix.Analysis.TypeChecking;
using Helix.Syntax;

namespace Helix.Analysis.Types {
    public record PointerType : HelixType {
        public HelixType InnerType { get; }

        public bool IsWritable { get; }

        public PointerType(HelixType innerType, bool isWritable) {
            this.InnerType = innerType;
            this.IsWritable = isWritable;
        }

        public override PassingSemantics GetSemantics(TypeFrame types) {
            return PassingSemantics.ReferenceType;
        }

        public override HelixType GetMutationSupertype(TypeFrame types) {
            return new PointerType(this.InnerType.GetMutationSupertype(types), true);
        }

        public override HelixType GetSignatureSupertype(TypeFrame types) {
            return this;
        }

        public override IEnumerable<HelixType> GetAccessibleTypes(TypeFrame frame) {
            yield return this;
            yield return this.InnerType;
        }

        public override string ToString() {
            if (this.IsWritable) {
                return this.InnerType + "*";
            }
            else {
                return "let " + this.InnerType + "*";
            }
        }
    }
}