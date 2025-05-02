using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record ReturnInstruction : IInstruction, ITerminalInstruction {
    public required Immediate ReturnValue { get; init; }
    
    public HelixType ReturnType => PrimitiveType.Void;

    public string[] Successors => [];

    public override string ToString() {
        return IInstruction.FormatOp("return", $"return {this.ReturnValue}");
    }

    public ITerminalInstruction RenameBlocks(IReadOnlyDictionary<string, string> newNames) {
        return this;
    }
}