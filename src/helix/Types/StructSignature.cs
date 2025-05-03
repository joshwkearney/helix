using Helix.TypeChecking;

namespace Helix.Types;

public record StructSignature(IReadOnlyList<StructMember> Members) {        
    public PassingSemantics GetSemantics(TypeFrame types) {
        if (this.Members.All(x => x.Type.GetSemantics(types) == PassingSemantics.ValueType)) {
            return PassingSemantics.ValueType;
        }
        else {
            return PassingSemantics.ContainsReferenceType;
        }
    }

    public IEnumerable<HelixType> GetAccessibleTypes(TypeFrame frame) {
        foreach (var mem in this.Members) {
            foreach (var type in mem.Type.GetAccessibleTypes(frame)) {
                yield return type;
            }
        }
    }
}

public record StructMember(string Name, HelixType Type) { }