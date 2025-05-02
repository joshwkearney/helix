using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record CreateLocalInstruction : IInstruction {
    public required HelixType ReturnType { get; init; }
    
    public required Immediate LocalName { get; init; }
    
    public override string ToString() {
        return IInstruction.FormatOp("local_create", $"var {this.LocalName} as {this.ReturnType}");
    }
}