using System.Diagnostics;

namespace Helix.FlowAnalysis;

public class Block {
    private List<IOp> phiNodes = [];
    private List<IOp> ops = [];
    private ITerminalOp? jump = null;
    
    public string Name { get; }
    
    public int Index { get; }

    public bool IsTerminated => this.jump != null;

    public ITerminalOp? Terminal => this.jump;

    public bool IsEmpty => this.ops.Count == 0;

    public IEnumerable<IOp> Instructions {
        get {
            foreach (var value in this.phiNodes) {
                yield return value;
            }

            foreach (var value in this.ops) {
                yield return value;
            }

            if (this.jump != null) {
                yield return this.jump;
            }
        }
    }
    
    public string[] Successors {
        get {
            if (this.jump == null) {
                return [];
            }
            else {
                return this.jump.Successors;
            }
        }
    }

    public Block(string name, int index) {
        this.Name = name;
        this.Index = index;
    }

    public void Terminate(ITerminalOp op) {
        this.jump = op;
    }

    public void Add(IOp op) {
        Debug.Assert(op is not ITerminalOp);
        
        this.ops.Add(op);
    }
}