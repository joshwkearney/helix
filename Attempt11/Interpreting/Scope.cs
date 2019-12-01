using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt10 {
    public class VariableInfo {
        public ITrophyType Type { get; }

        public IInterpretedValue Value { get; }

        public VariableInfo(ITrophyType type, IInterpretedValue value) {
            this.Type = type;
            this.Value = value;
        }
    }

    public class Scope {
        public ImmutableDictionary<string, VariableInfo> Variables { get; }

        public Scope() {
            this.Variables = ImmutableDictionary<string, VariableInfo>.Empty;
        }

        public Scope(ImmutableDictionary<string, VariableInfo> variables) {
            this.Variables = variables;
        }

        public Scope SetVariable(string name, ITrophyType type, IInterpretedValue value) {
            return new Scope(this.Variables.SetItem(name, new VariableInfo(type, value)));
        }
    }
}