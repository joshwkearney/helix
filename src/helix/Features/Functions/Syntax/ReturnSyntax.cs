using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.IRGeneration;
using Helix.Parsing;
using Helix.Parsing.IR;
using Helix.Syntax;

namespace Helix.Features.Functions.Syntax;

public record ReturnSyntax : ISyntax {
    public required TokenLocation Location { get; init; }

    public required ISyntax Operand { get; init; }
    
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