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
        return IOp.FormatOp("BinaryOp", $"{this.ReturnValue} as {this.ReturnType} = {this.Left} {this.Operation} {this.Right}");
    }
}