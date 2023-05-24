using Helix.Analysis.TypeChecking;
using Helix.Syntax;

namespace Helix.Analysis.Types {
    public record PointerType : HelixType {
        public HelixType InnerType { get; }

        public PointerType(HelixType innerType) {
            this.InnerType = innerType;
        }

        public override PassingSemantics GetSemantics(ITypedFrame types) {
            return PassingSemantics.ReferenceType;
        }
        
        public override string ToString() {
            return this.InnerType + "*";
        }

        public override IEnumerable<HelixType> GetContainedTypes(TypeFrame frame) {
            yield return this;
            yield return this.InnerType;
        }
    }
}