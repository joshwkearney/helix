using Helix.Analysis.TypeChecking;
using Helix.Syntax;

namespace Helix.Analysis.Types {
    public record ArrayType : HelixType {
        public HelixType InnerType { get; }

        public ArrayType(HelixType innerType) {
            this.InnerType = innerType;
        }

        public override PassingSemantics GetSemantics(ITypeContext types) {
            return PassingSemantics.ReferenceType;
        }

        public override string ToString() {
            return this.InnerType + "[]";
        }

        public override IEnumerable<HelixType> GetContainedTypes(TypeFrame frame) {
            yield return this;
            yield return this.InnerType;
        }

        public override HelixType GetMutationSupertype(ITypeContext types) => this;

        public override HelixType GetSignatureSupertype(ITypeContext types) => this;
    }
}