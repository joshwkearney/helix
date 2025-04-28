using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record SetMemberOp : IOp {
    public required Immediate Operand { get; init; }
    
    public required string MemberName { get; init; }
    
    public required Immediate MemberValue { get; init; }
    
    public required Immediate ReturnValue { get; init; }
    
    public required HelixType ReturnType { get; init; }

    public override string ToString() {
        return IOp.FormatOp("mem_set", $"let {this.ReturnValue} = {this.Operand} with {{ {this.MemberName} = {this.MemberValue} }}");
    }
}