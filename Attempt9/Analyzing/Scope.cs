using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt9 {
    public class VariableInfo {
        public ITrophyType Type { get; }

        public int DefinedClosureLevel { get; }

        public VariableInfo(ITrophyType type, int closureLevel) {
            this.Type = type;
            this.DefinedClosureLevel = closureLevel;
        }
    }

    public class Scope {
        public int ClosureLevel { get; } = 0;

        public ImmutableDictionary<string, VariableInfo> Variables { get; }

        public Scope() {
            this.ClosureLevel = 0;
            this.Variables = ImmutableDictionary<string, VariableInfo>.Empty;
        }

        public Scope(ImmutableDictionary<string, VariableInfo> vars, int closureLevel) {
            this.ClosureLevel = closureLevel;
            this.Variables = vars;
        }

        public Scope AddVariable(string name, VariableInfo info) {
            return new Scope(this.Variables.Add(name, info), this.ClosureLevel);
        }

        public Scope IncrementClosureLevel() {
            return new Scope(this.Variables, this.ClosureLevel + 1);
        }
    }
}