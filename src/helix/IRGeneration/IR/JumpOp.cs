using Helix.Analysis.Types;
using Helix.IRGeneration;

namespace Helix.Parsing.IR;

public record JumpOp : IOp, ITerminalOp {
    public required string BlockName { get; init; }
    
    public HelixType ReturnType => PrimitiveType.Void;

    public string[] Successors => [this.BlockName];

    public override string ToString() {
        return IOp.FormatOp("jump", $"goto {this.BlockName}");
    }

    public ITerminalOp RenameBlocks(IReadOnlyDictionary<string, string> newNames) {
        if (newNames.TryGetValue(this.BlockName, out var newName)) {
            return new JumpOp {
                BlockName = newName
            };
        }
        else {
            return this;
        }
    }
}