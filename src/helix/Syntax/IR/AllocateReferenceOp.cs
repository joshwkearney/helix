using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record AllocateReferenceOp : IOp {
    public required Immediate ReturnValue { get; init; }
    
    public required HelixType ReturnType { get; init; }
    
    public override string ToString() {
        return IOp.FormatOp("ref_malloc", $"let {this.ReturnValue} = malloc(&{this.ReturnType})");
    }
}