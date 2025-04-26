using Helix.Analysis;

namespace Helix.IRGeneration;

public class IRFrame {
    private readonly Dictionary<IdentifierPath, Immediate> variables = [];
    
    public string? ReturnBlockName { get; set; }

    public string? ContinueBlockName { get; set; } 
    
    public string? BreakBlockName { get; set; }

    public void SetVariable(IdentifierPath path, Immediate value) {
        this.variables[path] = value;
    }
    
    public Immediate GetVariable(IdentifierPath path) {
        return this.variables[path];
    }
}