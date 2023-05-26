using Helix.Analysis;
using Helix.Analysis.TypeChecking;

namespace Helix.Analysis.Types {
    public record StructType(IReadOnlyList<StructMember> Members) : HelixType {
        public override HelixType GetMutationSupertype(ITypeContext types) => this;

        public override HelixType GetSignatureSupertype(ITypeContext types) => this;

        public override PassingSemantics GetSemantics(ITypeContext types) {
            if (this.Members.All(x => x.Type.GetSemantics(types) == PassingSemantics.ValueType)) {
                return PassingSemantics.ValueType;
            }
            else {
                return PassingSemantics.ContainsReferenceType;
            }
        }
    }

    public record StructMember(string Name, HelixType Type, bool IsWritable) { }
}