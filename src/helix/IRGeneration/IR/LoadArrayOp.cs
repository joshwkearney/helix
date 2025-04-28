using Helix.Analysis.Types;
using Helix.IRGeneration;

namespace Helix.Parsing.IR;

public record LoadArrayOp : IOp {
    public required Immediate Array { get; init; }
    
    public required Immediate Index { get; init; }
    
    public required Immediate ReturnValue { get; init; }
    
    public required HelixType ReturnType { get; init; }

    public override string ToString() {
        return IOp.FormatOp("array_load", $"let {this.ReturnValue} <- {this.Array}[{this.Index}]");
    }
}