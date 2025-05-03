using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree;

public record ParseStructSignature {
    public required string Name { get; init; }

    public required IReadOnlyList<ParseStructMember> Members { get; init; }

    public required TokenLocation Location { get; init; }
        
    public StructSignature ResolveNames(TypeFrame types) {
        var mems = new List<StructMember>();

        foreach (var mem in this.Members) {
            if (!mem.MemberType.AsType(types).TryGetValue(out var type)) {
                throw TypeException.ExpectedTypeExpression(mem.Location);
            }

            mems.Add(new StructMember(mem.MemberName, type));
        }

        return new StructSignature(mems);
    }
}