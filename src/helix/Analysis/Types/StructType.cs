using Helix.Analysis;

namespace Helix.Analysis.Types {
    public record StructType(IReadOnlyList<StructMember> Members) : HelixType {
        public override HelixType GetMutationSupertype(ITypedFrame types) => this;

        public override HelixType GetSignatureSupertype(ITypedFrame types) => this;

        public override PassingSemantics GetSemantics(ITypedFrame types) {
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