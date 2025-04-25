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
        var doesBreak = !types.BreakFrames.IsEmpty;
        types = types.PopScope().ClearLoopFrames();

        var result = new LoopStatement {
            Location = this.Location,
            Body = body,
            AlwaysJumps = !doesBreak
        };

        return new TypeCheckResult(result, types);
    }
}