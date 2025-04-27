using Helix.Analysis.Types;
using Helix.IRGeneration;

namespace Helix.Parsing.IR;

public record LoadReferenceOp : IOp {
    public required Immediate Operand { get; init; }
    
    public required Immediate ReturnValue { get; init; }
    
    public required HelixType ReturnType { get; init; }

    public override string ToString() {
        return IOp.FormatOp("ref_load", $"var {this.ReturnValue} <- {this.Operand}");
    }
}