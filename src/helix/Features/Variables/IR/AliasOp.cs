using Helix.Analysis.Types;
using Helix.IRGeneration;

namespace Helix.Parsing.IR;

public record AliasOp : IOp {
    public required Immediate Value { get; init; }

    public required HelixType ReturnType { get; init; }
    
    public required Immediate NewValue { get; init; }
    
    public override string ToString() {
        return IOp.FormatOp("alias", $"var {this.NewValue} = {this.Value}");
    }
}