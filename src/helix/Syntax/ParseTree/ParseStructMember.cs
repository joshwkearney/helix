using Helix.Parsing;

namespace Helix.Syntax.ParseTree;

public record ParseStructMember {
    public required string MemberName { get; init; }

    public required IParseExpression MemberType { get; init; }

    public required TokenLocation Location { get; init; }
}