using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record UnaryOp : IOp {
    public Immediate Operand { get; init; }
    
    public UnaryOperatorKind Operation { get; init; }
    
    public HelixType ReturnType { get; init; }
    
    public Immediate ReturnValue { get; init; }

    public override string ToString() {
        return IOp.FormatOp("unary_op", $"let {this.ReturnValue} = {this.Operation} {this.Operand}");
    }
}