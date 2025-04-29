using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.FlowControl;

public record BlockTypedTree : ITypedStatement {
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