using Helix.FlowAnalysis;
using Helix.Types;

namespace Helix.Syntax.IR;

public record ReturnOp : IOp, ITerminalOp {
    public required Immediate ReturnValue { get; init; }
    
    public HelixType ReturnType => PrimitiveType.Void;

    public string[] Successors => [];

    public override string ToString() {
        return IOp.FormatOp("return", $"return {this.ReturnValue}");
    }

    public ITerminalOp RenameBlocks(IReadOnlyDictionary<string, string> newNames) {
        return this;
    }
}