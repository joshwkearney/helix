using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Functions;

public record ReturnTypedTree : ITypedTree {
    public required TokenLocation Location { get; init; }

    public required ITypedTree Operand { get; init; }
    
    public required FunctionType FunctionSignature { get; init; }

    public bool AlwaysJumps => true;
        
    public HelixType ReturnType => PrimitiveType.Void;

    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        if (this.FunctionSignature.ReturnType != PrimitiveType.Void) {
            writer.CurrentBlock.Add(new AssignLocalOp {
                LocalName = context.ReturnLocal!,
                Value = this.Operand.GenerateIR(writer, context)
            });
        }
        
        writer.CurrentBlock.Terminate(new JumpOp {
            BlockName = context.ReturnBlock!
        });

        return new Immediate.Void();
    }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        writer.WriteStatement(new CReturn() {
            Target = this.Operand.GenerateCode(types, writer)
        });

        return new CIntLiteral(0);
    }
}