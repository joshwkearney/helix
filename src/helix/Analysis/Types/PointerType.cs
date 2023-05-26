using Helix.Analysis.TypeChecking;
using Helix.Syntax;

namespace Helix.Analysis.Types {
    public record PointerType(HelixType InnerType, bool IsWritable) : HelixType {
        public override PassingSemantics GetSemantics(ITypeContext types) {
            return PassingSemantics.ReferenceType;
        }

        public override HelixType GetMutationSupertype(ITypeContext types) {
            return this;
        }

        public override HelixType GetSignatureSupertype(ITypeContext types) {
            return this;
        }

        public override IEnumerable<HelixType> GetContainedTypes(TypeFrame frame) {
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