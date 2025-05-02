using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record JumpConditionalInstruction : IInstruction, ITerminalInstruction {
    public required Immediate Condition { get; init; }
    
    public required string TrueBlockName { get; init; }
    
    public required string FalseBlockName { get; init; }
    
    public HelixType ReturnType => PrimitiveType.Void;

    public string[] Successors => [this.TrueBlockName, this.FalseBlockName];

    public override string ToString() {
        return IInstruction.FormatOp("jump_cond", $"goto {this.TrueBlockName} or {this.FalseBlockName} if {this.Condition}");
    }

    public ITerminalInstruction RenameBlocks(IReadOnlyDictionary<string, string> newNames) {
        var first = newNames.GetValueOrDefault(this.TrueBlockName, this.TrueBlockName);
        var second = newNames.GetValueOrDefault(this.FalseBlockName, this.FalseBlockName);

        if (first == this.TrueBlockName && second == this.FalseBlockName) {
            return this;
        }

        return new JumpConditionalInstruction {
            Condition = this.Condition,
            TrueBlockName = first,
            FalseBlockName = second
        };
    }
}