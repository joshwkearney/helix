using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record AllocateArrayInstruction : IInstruction {
    public required Immediate ReturnValue { get; init; }
    
    public required HelixType InnerType { get; init; }
    
    public required Immediate Length { get; init; }

    public override string ToString() {
        return IInstruction.FormatOp("array_malloc", $"let {this.ReturnValue} as {new ArrayType(this.InnerType)} = alloc {this.InnerType}[{this.Length}]");
    }
}