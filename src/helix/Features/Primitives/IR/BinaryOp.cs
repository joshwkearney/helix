using Helix.Analysis.Types;
using Helix.IRGeneration;

namespace Helix.Features.Primitives.IR;

public record BinaryOp : IOp {
    public Immediate Left { get; init; }
    
    public Immediate Right { get; init; }
    
    public BinaryOperationKind Operation { get; init; }
    
    public HelixType ReturnType { get; init; }
    
    public Immediate ReturnValue { get; init; }

    public override string ToString() {
        return IOp.FormatOp("binary_op", $"var {this.ReturnValue} = {this.Left} {this.Operation.GetSymbol()} {this.Right}");
    }
}