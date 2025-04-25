using Helix.Analysis.TypeChecking;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.FlowControl;

public record LoopParseStatement : IParseSyntax {
    public required TokenLocation Location { get; init; }

    public required IParseSyntax Body { get; init; }
        
    public bool IsPure => false;

    public ISyntax CheckTypes(TypeFrame types) {
        var bodyTypes = new TypeFrame(types, "$loop");
        var body = this.Body.CheckTypes(bodyTypes).ToRValue(bodyTypes);

        var result = new LoopStatement {
            Location = this.Location,
            Body = body
        };

        return result;
    }
}