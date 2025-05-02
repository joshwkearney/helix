using System.Diagnostics;

namespace Helix.FlowAnalysis;

public class Block {
    private List<IInstruction> phiNodes = [];
    private List<IInstruction> ops = [];
    private ITerminalInstruction? jump = null;
    
    public string Name { get; }
    
    public int Index { get; }

    public bool IsTerminated => this.jump != null;

    public ITerminalInstruction? Terminal => this.jump;

    public bool IsEmpty => this.ops.Count == 0;

    public IEnumerable<IInstruction> Instructions {
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

    public void Terminate(ITerminalInstruction instruction) {
        this.jump = instruction;
    }

    public void Add(IInstruction instruction) {
        Debug.Assert(instruction is not ITerminalInstruction);
        
        this.ops.Add(instruction);
    }
}