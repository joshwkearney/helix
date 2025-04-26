namespace Helix.IRGeneration;

public class Block {
    public string Name { get; init; }
    
    public IReadOnlyList<IOp> Ops { get; init; }
}