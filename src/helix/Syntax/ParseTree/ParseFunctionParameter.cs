using Helix.Parsing;

namespace Helix.Syntax.ParseTree;

public record ParseFunctionParameter {
    public required string Name { get; init; }

    public required IParseExpression Type { get; init; }

    public required TokenLocation Location { get; init; }
}