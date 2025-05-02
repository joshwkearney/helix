namespace Helix.FlowAnalysis;

public interface IInstruction {
    public static string FormatOp(string name, string text) {
        return name.PadRight(15) + " | " + text;
    }
}

public interface ITerminalInstruction : IInstruction {
    public string[] Successors { get; }

    public ITerminalInstruction RenameBlocks(IReadOnlyDictionary<string, string> newNames);
}


























