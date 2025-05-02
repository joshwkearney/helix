using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record AssignMemberOp : IInstruction {
    public required Immediate LocalName { get; init; }
    
    public required Immediate Value { get; init; }

    public required string MemberName { get; init; }
    
    public override string ToString() {
        return IInstruction.FormatOp("member_assign", $"{this.LocalName}.{this.MemberName} = {this.Value}");
    }
}