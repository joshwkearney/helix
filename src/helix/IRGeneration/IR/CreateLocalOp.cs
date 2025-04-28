using Helix.Analysis.Types;
using Helix.IRGeneration;

namespace Helix.Parsing.IR;

public record CreateLocalOp : IOp {
    public required HelixType ReturnType { get; init; }
    
    public required Immediate LocalName { get; init; }
    
    public override string ToString() {
        return IOp.FormatOp("local_create", $"var {this.LocalName}");
    }
}