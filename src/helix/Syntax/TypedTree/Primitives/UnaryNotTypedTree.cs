using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Primitives;

public record UnaryNotTypedTree : ITypedTree {
    public required TokenLocation Location { get; init; }

    public required HelixType ReturnType { get; init; }
    
    public required ITypedTree Operand { get; init; }
    
    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return new CNot {
            Target = this.Operand.GenerateCode(types, writer)
        };
    }

    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        var operand = this.Operand.GenerateIR(writer, context);
        var name = writer.GetName();
        
        writer.CurrentBlock.Add(new UnaryOp {
            Operation = UnaryOperatorKind.Not,
            Operand = operand,
            ReturnValue = name,
            ReturnType = this.ReturnType
        });

        return name;
    }
}