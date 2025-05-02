using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record AllocateReferenceInstruction : IInstruction {
    public required Immediate ReturnValue { get; init; }
    
    public required HelixType InnerType { get; init; }
    
    public override string ToString() {
        return IInstruction.FormatOp("ref_malloc", $"let {this.ReturnValue} as {new ReferenceType(this.InnerType)} = alloc &{this.InnerType}");
    }
}