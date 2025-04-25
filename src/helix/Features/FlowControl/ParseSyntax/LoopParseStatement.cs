using Helix.Analysis.TypeChecking;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.FlowControl;

public record LoopParseStatement : IParseSyntax {
    public required TokenLocation Location { get; init; }

    public required IParseSyntax Body { get; init; }
        
    public bool IsPure => false;

    public TypeCheckResult CheckTypes(TypeFrame types) {
        types = types.WithScope("$loop");
        (var body, types) = this.Body.CheckTypes(types);
        types = types.PopScope();

        var result = new LoopStatement {
            Location = this.Location,
            Body = body
        };

        return new TypeCheckResult(result, types);
    }
}