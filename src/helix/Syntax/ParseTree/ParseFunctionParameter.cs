using Helix.Parsing;

namespace Helix.Syntax.ParseTree;

public record ParseFunctionParameter {
    public required string Name { get; init; }

    public required IParseTree Type { get; init; }

    public required TokenLocation Location { get; init; }
}