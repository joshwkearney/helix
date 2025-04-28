using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.FlowControl;

public record BlockTypedTree : ITypedTree {
    public static ITypedTree FromMany(TokenLocation loc, IReadOnlyList<ITypedTree> stats) {
        if (stats.Count == 0) {
            return new VoidLiteral { Location = loc };
        }
        else if (stats.Count == 1) {
            return stats[0];
        }
        else {
            return stats
                .Reverse()
                .Aggregate((x, y) => new BlockTypedTree() {
                    Location = y.Location.Span(x.Location),
                    First = y,
                    Second = x,
                    AlwaysJumps = y.AlwaysJumps || x.AlwaysJumps
                });
        }
    }

    public required TokenLocation Location { get; init; }

    public required ITypedTree First { get; init; }
        
    public required ITypedTree Second { get; init; }
    
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