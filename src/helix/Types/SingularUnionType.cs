using Helix.Collections;
using Helix.TypeChecking;

namespace Helix.Types;

public record SingularUnionType : HelixType {
    public required HelixType UnionType { get; init; }

    public required UnionType UnionSignature { get; init; }
    
    public required ValueSet<string> MemberNames { get; init; }

    public override PassingSemantics GetSemantics(TypeFrame types) {
        return this.UnionType.GetSemantics(types);
    }

    public override HelixType GetSignature(TypeFrame types) {
        return this.UnionType;
    }

    public override Option<UnionType> AsUnion(TypeFrame types) {
        return this.UnionSignature;
    }
}