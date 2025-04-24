using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Functions;

public record ParseFunctionParameter {
    public required string Name { get; init; }

    public required IParseSyntax Type { get; init; }

    public required TokenLocation Location { get; init; }
}