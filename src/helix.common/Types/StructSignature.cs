using Helix.Common.Collections;

namespace Helix.Common.Types {
    public record StructSignature {
        public ValueList<StructMember> Members { get; init; } = [];
    }

    public record StructMember {
        public required string Name { get; init; }

        public required IHelixType Type { get; init; }

        public required bool IsMutable { get; init; }
    }
}