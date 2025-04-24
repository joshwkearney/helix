using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Structs;

public record ParseStructMember {
    public required string MemberName { get; init; }

    public required IParseSyntax MemberType { get; init; }

    public required TokenLocation Location { get; init; }
}