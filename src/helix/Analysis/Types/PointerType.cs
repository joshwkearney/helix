using Helix.Analysis.TypeChecking;
using Helix.Syntax;

namespace Helix.Analysis.Types {
    public record PointerType(HelixType InnerType, bool IsWritable) : HelixType {
        public override PassingSemantics GetSemantics(ITypedFrame types) {
            return PassingSemantics.ReferenceType;
        }

        public override string ToString() {
            if (this.IsWritable) {
                return this.InnerType + "*";
            }
            else {
                return "let " + this.InnerType + "*";
            }
        }

        public override IEnumerable<HelixType> GetContainedTypes(TypeFrame frame) {
            yield return this;
            yield return this.InnerType;
        }
    }
}