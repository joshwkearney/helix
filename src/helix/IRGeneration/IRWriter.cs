namespace Helix.IRGeneration;

public class IRWriter {
    private readonly Dictionary<string, int> variableVersions = [];
    private readonly Dictionary<string, int> blockVersions = [];
    private readonly List<Block> blocks = [];
    
    private List<IOp> ops = [];
    private int tempCounter = 1;

    public Immediate GetName(string name) {
        if (!this.variableVersions.TryGetValue(name, out var version)) {
            this.variableVersions[name] = version = 1;
        }

        string result;
        if (this.variableVersions[name] == 1) {
            result = name;
        }
        else {
            result = name + "%" + this.variableVersions[name];
        }
        
        this.variableVersions[name]++;
        return new Immediate.Name(result);
    }

    public Immediate GetName() {
        var result = "%" + this.tempCounter;

        this.tempCounter++;
        return new Immediate.Name(result);
    }

    public void WriteOp(IOp op) {
        this.ops.Add(op);
    }

    public string GetBlockName(string name) {
        if (!this.blockVersions.TryGetValue(name, out var version)) {
            this.variableVersions[name] = version = 1;
        }

        string result;
        if (this.blockVersions[name] == 1) {
            result = name;
        }
        else {
            result = name + "%" + this.blockVersions[name];
        }
        
        this.blockVersions[name]++;
        return result;
    }

    public void PopBlock(string name) {
        this.blocks.Add(new Block {
            Name = name,
            Ops = this.ops
        });

        this.ops = [];
    }

    public override string ToString() {
        return string.Join(Environment.NewLine, this.ops);
    }
}