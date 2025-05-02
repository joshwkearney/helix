using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record GetMemberInstruction : IInstruction {
    public required Immediate Operand { get; init; }
    
    public required string MemberName { get; init; }
    
    public required Immediate ReturnValue { get; init; }
    
    public required HelixType ReturnType { get; init; }

    public override string ToString() {
        return IInstruction.FormatOp("mem_get", $"let {this.ReturnValue} = {this.Operand}.{this.MemberName}");
    }
}