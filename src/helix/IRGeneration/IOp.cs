namespace Helix.IRGeneration;

public interface IOp {
    public static string FormatOp(string name, string text) {
        return name.PadRight(10) + " | " + text;
    }
}