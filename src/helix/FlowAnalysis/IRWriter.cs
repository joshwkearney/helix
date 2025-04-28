using System.Diagnostics;

namespace Helix.FlowAnalysis;

public class IRWriter {
    private readonly Dictionary<string, int> variableVersions = [];
    private readonly Dictionary<string, int> blockVersions = [];
    private readonly Dictionary<string, Block> blocks = [];

    private Block? currentBlock = null;
    private int blockIndex = 0;
    private int tempCounter = 1;

    public Block CurrentBlock {
        get {
            Debug.Assert(this.currentBlock != null);
            
            return this.currentBlock;
        }
    }

    public Dictionary<string, Block> Blocks => this.blocks;

    public Immediate GetName(string name) {
        if (!this.variableVersions.TryGetValue(name, out var version)) {
            this.variableVersions[name] = version = 1;
        }

        string result;
        if (version == 1) {
            result = name;
        }
        else {
            result = name + "$" + version;
        }
        
        this.variableVersions[name]++;
        return new Immediate.Name(result);
    }

    public Immediate GetName() {
        var result = "$" + this.tempCounter;

        this.tempCounter++;
        return new Immediate.Name(result);
    }

    public string GetBlockName(string name) {
        if (!this.blockVersions.TryGetValue(name, out var version)) {
            this.blockVersions[name] = version = 1;
        }

        string result;
        if (version == 1) {
            result = name;
        }
        else {
            result = name + "$" + version;
        }
        
        this.blockVersions[name]++;
        return result;
    }

    public void PushBlock(string name) {
        Debug.Assert(this.currentBlock == null);

        this.currentBlock = new Block(name, this.blockIndex++);
    }

    public void PopBlock() {
        Debug.Assert(this.currentBlock != null);

        this.blocks.Add(this.CurrentBlock.Name, this.currentBlock);
        this.currentBlock = null;
    }
}