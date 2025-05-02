using Helix.FlowAnalysis;

namespace Helix.Syntax.IR;

public record AssignLocalInstruction : IInstruction {
    public required Immediate Value { get; init; }

    public required Immediate LocalName { get; init; }
    
    public override string ToString() {
        return IInstruction.FormatOp("local_assign", $"{this.LocalName} = {this.Value}");
    }
}