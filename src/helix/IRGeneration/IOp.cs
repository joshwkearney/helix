using Helix.Analysis.Types;

namespace Helix.IRGeneration;

public interface IOp {
    public static string FormatOp(string name, string text) {
        return name.PadRight(15) + " | " + text;
    }
}

public interface ITerminalOp : IOp {
    public string[] Successors { get; }

    public ITerminalOp RenameBlocks(IReadOnlyDictionary<string, string> newNames);
}