using Helix.TypeChecking;

namespace Helix.Types;

public record ArrayType : HelixType {
    public HelixType InnerType { get; }

    public ArrayType(HelixType innerType) {
        this.InnerType = innerType;
    }

    public override PassingSemantics GetSemantics(TypeFrame types) {
        return PassingSemantics.ReferenceType;
    }

    public override string ToString() {
        return this.InnerType + "[]";
    }

    public override IEnumerable<HelixType> GetAccessibleTypes(TypeFrame frame) {
        yield return this;
        yield return this.InnerType;
    }

    public override Option<ArrayType> AsArray(TypeFrame types) => this;

    public override HelixType GetSupertype(TypeFrame types) => this;
}