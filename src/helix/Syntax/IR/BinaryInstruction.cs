using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record BinaryInstruction : IInstruction {
    public Immediate Left { get; init; }
    
    public Immediate Right { get; init; }
    
    public BinaryOperationKind Operation { get; init; }
    
    public HelixType ReturnType { get; init; }
    
    public Immediate ReturnValue { get; init; }

    public override string ToString() {
        return IInstruction.FormatOp("binary_op", $"let {this.ReturnValue} = {this.Left} {this.Operation.GetSymbol()} {this.Right}");
    }
}