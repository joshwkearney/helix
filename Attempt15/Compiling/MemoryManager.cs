using JoshuaKearney.Attempt15.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace JoshuaKearney.Attempt15.Compiling {
    public class MemoryBlock {
        public IList<IdentifierInfo> CountedVariables { get; }

        public MemoryBlock() {
            this.CountedVariables = new List<IdentifierInfo>();
        }

        public MemoryBlock(IEnumerable<IdentifierInfo> vars) {
            this.CountedVariables = vars.ToList();
        }
    }

    public class MemoryManager {
        private readonly ICodeGenerator gen;

        public Dictionary<ITrophyType, string> ReferenceIncrementors { get; } = new Dictionary<ITrophyType, string>();
        public Dictionary<ITrophyType, string> ReferenceDecrementors { get; } = new Dictionary<ITrophyType, string>();

        public Stack<MemoryBlock> MemoryBlocks { get; } = new Stack<MemoryBlock>();

        public MemoryManager(ICodeGenerator gen) {
            this.gen = gen;
        }

        public void IncrementValue(ITrophyType type, string value) {
            var call = gen.FunctionCall(this.ReferenceIncrementors[type], new[] { value });
            gen.CodeBlocks.Peek().Add(call + ";");
        }

        public void DecrementValue(ITrophyType type, string value) {
            var call = gen.FunctionCall(this.ReferenceDecrementors[type], new[] { value });
            gen.CodeBlocks.Peek().Add(call + ";");
        }

        public void OpenMemoryBlock() {
            this.MemoryBlocks.Push(new MemoryBlock());
        }

        public void OpenMemoryBlock(IEnumerable<IdentifierInfo> varsToTrack) {
            this.MemoryBlocks.Push(new MemoryBlock(varsToTrack));
        }

        public void CloseMemoryBlock() {
            var block = this.MemoryBlocks.Pop();

            foreach (var info in block.CountedVariables) {
                this.DecrementValue(info.Type, info.Name);
            }
        }

        public void CleanupMemoryBlock(IEnumerable<IdentifierInfo> usedVars) {
            var cleanup = this.MemoryBlocks.Peek().CountedVariables.Except(usedVars).ToArray();

            foreach (var var in cleanup) {
                this.DecrementValue(var.Type, var.Name);
                this.MemoryBlocks.Peek().CountedVariables.Remove(var);
            }
        }

        public void RegisterVariable(string name, ITrophyType type) {
            this.MemoryBlocks.Peek().CountedVariables.Add(new IdentifierInfo(name, type));
        }

        public void RegisterVariable(IdentifierInfo info) {
            this.MemoryBlocks.Peek().CountedVariables.Add(info);
        }

        public void UnregisterVariable(string name, ITrophyType type) {
            this.MemoryBlocks.Peek().CountedVariables.Remove(new IdentifierInfo(name, type));
        }

        public void UnregisterVariable(IdentifierInfo info) {
            this.MemoryBlocks.Peek().CountedVariables.Remove(info);
        }
    }
}