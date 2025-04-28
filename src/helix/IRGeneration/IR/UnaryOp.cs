using Helix.Analysis.Types;
using Helix.IRGeneration;

namespace Helix.Features.Primitives.IR;

public record UnaryOp : IOp {
    public Immediate Operand { get; init; }
    
    public UnaryOperatorKind Operation { get; init; }
    
    public HelixType ReturnType { get; init; }
    
    public Immediate ReturnValue { get; init; }

    public override string ToString() {
        return IOp.FormatOp("unary_op", $"let {this.ReturnValue} = {this.Operation} {this.Operand}");
    }
}