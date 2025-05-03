using Helix.Collections;
using Helix.TypeChecking;

namespace Helix.Types;

public record SingularUnionType : HelixType {
    public required HelixType UnionType { get; init; }

    public required UnionSignature UnionSignature { get; init; }
    
    public required ValueSet<string> MemberNames { get; init; }

    public override PassingSemantics GetSemantics(TypeFrame types) {
        return this.UnionType.GetSemantics(types);
    }


    public override Option<UnionSignature> AsUnion(TypeFrame types) {
        return this.UnionSignature;
    }

    public override HelixType GetSupertype(TypeFrame types) => this.UnionType;
}