using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Variables;

public record TypedVariableAccessExpression : ITypedExpression {
    public required TokenLocation Location { get; init; }
    
    public required IdentifierPath VariablePath { get; init; }
    
    public required HelixType ReturnType { get; init; }

    public ILValue ToLValue(TypeFrame types) {
        return new ILValue.Local(this.VariablePath, new NominalType(this.VariablePath, NominalTypeKind.Variable));
    }
    
    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        if (context.AllocatedVariables.Contains(this.VariablePath)) {
            // If this variable is opaque, it is storing a reference and we need to load from it
            var temp = writer.GetName();

            writer.CurrentBlock.Add(new LoadReferenceOp {
                Operand = context.GetVariable(this.VariablePath),
                ReturnType = this.ReturnType,
                ReturnValue = temp
            });

            return temp;
        }
        else {
            return context.GetVariable(this.VariablePath);
        }
    }

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        ICSyntax result = new CVariableLiteral(writer.GetVariableName(this.VariablePath));

        if (writer.VariableKinds[this.VariablePath] == CVariableKind.Allocated) {
            result = new CPointerDereference {
                Target = result
            };
        }

        return result;
    }
}