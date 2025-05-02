using Helix.FlowAnalysis;

namespace Helix.Syntax.IR;

public record StoreInstruction : IInstruction {
    public required Immediate Value { get; init; }

    public required Immediate Reference { get; init; }
    
    public override string ToString() {
        return IInstruction.FormatOp("store", $"{this.Value} -> [ {this.Reference} ]");
    }
}