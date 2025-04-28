using Helix.Analysis.Types;
using Helix.IRGeneration;

namespace Helix.Parsing.IR;

public record AllocateOp : IOp {
    public required Immediate ReturnValue { get; init; }
    
    public required HelixType ReturnType { get; init; }

    public override string ToString() {
        return IOp.FormatOp("ref_malloc", $"let {this.ReturnValue} = malloc(&{this.ReturnType})");
    }
}