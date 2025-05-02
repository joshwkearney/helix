using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record JumpInstruction : IInstruction, ITerminalInstruction {
    public required string BlockName { get; init; }
    
    public HelixType ReturnType => PrimitiveType.Void;

    public string[] Successors => [this.BlockName];

    public override string ToString() {
        return IInstruction.FormatOp("jump", $"goto {this.BlockName}");
    }

    public ITerminalInstruction RenameBlocks(IReadOnlyDictionary<string, string> newNames) {
        if (newNames.TryGetValue(this.BlockName, out var newName)) {
            return new JumpInstruction {
                BlockName = newName
            };
        }
        else {
            return this;
        }
    }
}