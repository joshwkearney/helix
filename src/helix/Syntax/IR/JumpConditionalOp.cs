using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record JumpConditionalOp : IOp, ITerminalOp {
    public required Immediate Condition { get; init; }
    
    public required string TrueBlockName { get; init; }
    
    public required string FalseBlockName { get; init; }
    
    public HelixType ReturnType => PrimitiveType.Void;

    public string[] Successors => [this.TrueBlockName, this.FalseBlockName];

    public override string ToString() {
        return IOp.FormatOp("jump_cond", $"goto {this.TrueBlockName} or {this.FalseBlockName} if {this.Condition}");
    }

    public ITerminalOp RenameBlocks(IReadOnlyDictionary<string, string> newNames) {
        var first = newNames.GetValueOrDefault(this.TrueBlockName, this.TrueBlockName);
        var second = newNames.GetValueOrDefault(this.FalseBlockName, this.FalseBlockName);

        if (first == this.TrueBlockName && second == this.FalseBlockName) {
            return this;
        }

        return new JumpConditionalOp {
            Condition = this.Condition,
            TrueBlockName = first,
            FalseBlockName = second
        };
    }
}