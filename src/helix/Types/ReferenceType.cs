using Helix.TypeChecking;

namespace Helix.Types;

public record ReferenceType : HelixType {
    public HelixType InnerType { get; }

    public ReferenceType(HelixType innerType) {
        this.InnerType = innerType;
    }

    public override PassingSemantics GetSemantics(TypeFrame types) {
        return PassingSemantics.ReferenceType;
    }

    public override HelixType GetSignature(TypeFrame types) {
        return new ReferenceType(this.InnerType.GetSignature(types));
    }

    public override Option<ReferenceType> AsReference(TypeFrame types) => this;

    public override IEnumerable<HelixType> GetAccessibleTypes(TypeFrame frame) {
        yield return this;
        yield return this.InnerType;
    }

    public override string ToString() {
        return "&" + this.InnerType;
    }
}