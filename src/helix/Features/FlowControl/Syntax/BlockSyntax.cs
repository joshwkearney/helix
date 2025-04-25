using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.FlowControl;

public record BlockSyntax : ISyntax {
    public static ISyntax FromMany(TokenLocation loc, IReadOnlyList<ISyntax> stats) {
        if (stats.Count == 0) {
            return new VoidLiteral { Location = loc };
        }
        else if (stats.Count == 1) {
            return stats[0];
        }
        else {
            return stats
                .Reverse()
                .Aggregate((x, y) => new BlockSyntax() {
                    Location = y.Location.Span(x.Location),
                    First = y,
                    Second = x
                });
        }
    }

    public required TokenLocation Location { get; init; }

    public required ISyntax First { get; init; }
        
    public required ISyntax Second { get; init; }

    public HelixType ReturnType => this.Second.ReturnType;

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        this.First.GenerateCode(types, writer);
            
        return this.Second.GenerateCode(types, writer);
    }
}