using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Arrays;

public record TypedArrayIndexExpression : ITypedExpression {
    public required ArrayType ArraySignature { get; init; }
        
    public required ITypedExpression Operand { get; init; }
        
    public required ITypedExpression Index { get; init; }
        
    public TokenLocation Location => this.Operand.Location;

    public HelixType ReturnType => this.ArraySignature.InnerType;

    public ILValue ToLValue(TypeFrame types) {
        return new ILValue.ArrayIndex(this.Operand, this.Index, this.ArraySignature.InnerType);
    }
        
    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        var array = this.Operand.GenerateIR(writer, context);
        var index = this.Index.GenerateIR(writer, context);
        var temp1 = writer.GetName();
        var temp2 = writer.GetName();
        
        writer.CurrentBlock.Add(new GetArrayOffsetInstruction() {
            Array = array,
            Index = index,
            ReturnValue = temp1,
            ReturnType = new ReferenceType(this.ReturnType),
        });
            
        writer.CurrentBlock.Add(new LoadInstruction() {
            Operand = temp1,
            ReturnValue = temp2,
            ReturnType = this.ReturnType
        });
            
        return temp2;
    }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        var target = this.Operand.GenerateCode(types, writer);
            
        return new CPointerDereference {
            Target = new CBinaryExpression {
                Left = new CMemberAccess {
                    Target = target,
                    MemberName = "data"
                },
                Right = this.Index.GenerateCode(types, writer),
                Operation = BinaryOperationKind.Add
            }
        };
    }
}