using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Functions;

public record TypedReturnStatement : ITypedStatement {
    public required TokenLocation Location { get; init; }

    public required ITypedExpression Operand { get; init; }
    
    public required FunctionType FunctionSignature { get; init; }

    public bool AlwaysJumps => true;
        
    public HelixType ReturnType => PrimitiveType.Void;

    public void GenerateIR(IRWriter writer, IRFrame context) {
        if (this.FunctionSignature.ReturnType != PrimitiveType.Void) {
            var value = this.Operand.GenerateIR(writer, context);
            
            writer.CurrentBlock.Add(new AssignLocalInstruction {
                LocalName = context.ReturnLocal,
                Value = value
            });
        }
        
        writer.CurrentBlock.Terminate(new JumpInstruction {
            BlockName = context.ReturnBlock
        });
    }

    public void GenerateCode(TypeFrame types, ICStatementWriter writer) {
        writer.WriteStatement(new CReturn {
            Target = this.Operand.GenerateCode(types, writer)
        });
    }
}