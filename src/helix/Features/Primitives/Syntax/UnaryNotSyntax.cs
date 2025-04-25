using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;
namespace Helix.Features.Primitives;

public record UnaryNotSyntax : ISyntax {
    public required TokenLocation Location { get; init; }

    public required HelixType ReturnType { get; init; }
    
    public required ISyntax Operand { get; init; }
    
    public required bool AlwaysJumps { get; init; }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return new CNot() {
            Target = this.Operand.GenerateCode(types, writer)
        };
    }
}