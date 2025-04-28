using System.Collections.Immutable;
using System.Diagnostics;
using Helix.TypeChecking;

namespace Helix.FlowAnalysis;

public class IRFrame {
    private readonly Stack<(string continueBlock, string breakBlock)> loops = [];
    private readonly Stack<(string returnBlock, Immediate returnLocal)> functions = [];
    private readonly Dictionary<IdentifierPath, Immediate> variables = [];

    public string ReturnBlock {
        get {
            Debug.Assert(this.functions.Count > 0);

            return this.functions.Peek().returnBlock;
        }
    }
    
    public Immediate ReturnLocal {
        get {
            Debug.Assert(this.functions.Count > 0);

            return this.functions.Peek().returnLocal;
        }
    }


    public string ContinueBlock {
        get {
            Debug.Assert(this.loops.Count > 0);

            return this.loops.Peek().continueBlock;
        }
    }

    public string BreakBlock {
        get {
            Debug.Assert(this.loops.Count > 0);

            return this.loops.Peek().breakBlock;
        }
    }
    
    public ImmutableHashSet<IdentifierPath> AllocatedVariables { get; }

    public IRFrame(ImmutableHashSet<IdentifierPath> allocated) {
        this.AllocatedVariables = allocated;
    }

    public void SetVariable(IdentifierPath path, Immediate value) {
        this.variables[path] = value;
    }
    
    public Immediate GetVariable(IdentifierPath path) {
        return this.variables[path];
    }

    public void PushLoop(string breakBlock, string continueBlock) {
        this.loops.Push((continueBlock, breakBlock));
    }

    public void PopLoop() {
        this.loops.Pop();
    }

    public void PushFunction(string returnBlock, Immediate returnLocal) {
        this.functions.Push((returnBlock, returnLocal));
    }

    public void PopFunction() {
        this.functions.Pop();
    }
}