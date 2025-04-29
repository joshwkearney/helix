using Helix.CodeGeneration;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.TypeChecking;

namespace Helix.Syntax.TypedTree.FlowControl;

public record TypedBlockStatement : ITypedStatement {
    public required TokenLocation Location { get; init; }

    public required IReadOnlyList<ITypedStatement> Statements { get; init; }
    
    public required bool AlwaysJumps { get; init; }
    
    public void GenerateIR(IRWriter writer, IRFrame context) {
        foreach (var stat in this.Statements) {
            stat.GenerateIR(writer, context);
        }
    }

    public void GenerateCode(TypeFrame types, ICStatementWriter writer) {
        foreach (var stat in this.Statements) {
            stat.GenerateCode(types, writer);
        }
    }
}