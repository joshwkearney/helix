using Helix.Common.Collections;

namespace Helix.Common.Types {
    public record UnionSignature {
        public ValueList<UnionMember> Members { get; init; } = [];
    }

    public record UnionMember {
        public required string Name { get; init; }

        public required IHelixType Type { get; init; }

        public required bool IsMutable { get; init; }
    }
}