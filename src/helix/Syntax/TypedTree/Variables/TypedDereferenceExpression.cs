using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Variables;

public record TypedDereferenceExpression : ITypedExpression {
    public required TokenLocation Location { get; init; }

    public required ITypedExpression Operand { get; init; }
        
    public required ReferenceType OperandSignature { get; init; }
        
    public HelixType ReturnType => this.OperandSignature.InnerType;

    public ILValue ToLValue(TypeFrame types) {
        return new ILValue.Dereference(this.Operand, this.OperandSignature.InnerType);
    }
        
    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        var temp = writer.GetName();
        
        writer.CurrentBlock.Add(new LoadReferenceOp {
            Operand = this.Operand.GenerateIR(writer, context),
            ReturnType = this.ReturnType,
            ReturnValue = temp
        });

        return temp;
    }
        
    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        var target = this.Operand.GenerateCode(types, writer);
        var pointerType = this.Operand.AssertIsReference(types);
        var tempName = writer.GetVariableName();
        var tempType = writer.ConvertType(pointerType.InnerType, types);

        writer.WriteEmptyLine();
        writer.WriteComment($"Line {this.Location.Line}: Pointer dereference");
            
        writer.WriteStatement(new CVariableDeclaration {
            Name = tempName,
            Type = tempType,
            Assignment = new CPointerDereference {
                Target = new CMemberAccess {
                    Target = target,
                    MemberName = "data",
                    IsPointerAccess = false
                }
            }
        });

        writer.WriteEmptyLine();

        return new CVariableLiteral(tempName);
    }
}