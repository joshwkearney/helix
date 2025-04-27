using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives.Syntax;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.IRGeneration;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.FlowControl.Syntax;

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
                    Second = x,
                    AlwaysJumps = y.AlwaysJumps || x.AlwaysJumps
                });
        }
    }

    public required TokenLocation Location { get; init; }

    public required ISyntax First { get; init; }
        
    public required ISyntax Second { get; init; }
    
    public required bool AlwaysJumps { get; init; }

    public HelixType ReturnType => this.Second.ReturnType;
    
    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        this.First.GenerateIR(writer, context);

        return this.Second.GenerateIR(writer, context);
    }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        this.First.GenerateCode(types, writer);
            
        return this.Second.GenerateCode(types, writer);
    }
}