using Helix.IRGeneration;

namespace Helix.Parsing.IR;

public record AssignLocalOp : IOp {
    public required Immediate Value { get; init; }

    public required Immediate LocalName { get; init; }
    
    public override string ToString() {
        return IOp.FormatOp("local_assign", $"{this.LocalName} = {this.Value}");
    }
}