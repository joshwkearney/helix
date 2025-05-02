using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record GetArrayOffsetInstruction : IInstruction {
    public required Immediate Array { get; init; }
    
    public required Immediate Index { get; init; }
    
    public required Immediate ReturnValue { get; init; }
    
    public required HelixType ReturnType { get; init; }

    public override string ToString() {
        return IInstruction.FormatOp("offset_array", $"let {this.ReturnValue} as {this.ReturnType} = &( {this.Array}[{this.Index}] )");
    }
}