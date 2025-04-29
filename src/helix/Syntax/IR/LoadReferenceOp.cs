using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record LoadReferenceOp : IOp {
    public required Immediate Operand { get; init; }
    
    public required Immediate ReturnValue { get; init; }
    
    public required HelixType ReturnType { get; init; }

    public override string ToString() {
        return IOp.FormatOp("ref_load", $"let {this.ReturnValue} <- [ {this.Operand} ]");
    }
}