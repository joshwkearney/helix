using Helix.IRGeneration;

namespace Helix.Parsing.IR;

public record AssignmentOp : IOp {
    public required Immediate Value { get; init; }

    public required Immediate Variable { get; init; }
    
    public override string ToString() {
        return IOp.FormatOp("assign", $"{this.Variable} = {this.Value}");
    }
}