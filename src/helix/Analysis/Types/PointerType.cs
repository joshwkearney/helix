using Helix.Analysis.TypeChecking;

namespace Helix.Analysis.Types {
    public record PointerType : HelixType {
        public HelixType InnerType { get; }

        public PointerType(HelixType innerType) {
            this.InnerType = innerType;
        }

        public override PassingSemantics GetSemantics(TypeFrame types) {
            return PassingSemantics.ReferenceType;
        }

        public override HelixType GetSignature(TypeFrame types) {
            return new PointerType(this.InnerType.GetSignature(types));
        }

        public override Option<PointerType> AsVariable(TypeFrame types) => this;

        public override IEnumerable<HelixType> GetAccessibleTypes(TypeFrame frame) {
            yield return this;
            yield return this.InnerType;
        }

        public override string ToString() {
            return "&" + this.InnerType;
        }
    }
}