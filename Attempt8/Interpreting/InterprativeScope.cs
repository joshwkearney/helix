using Attempt12.Analyzing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt12.Interpreting {
    public class InterprativeScope {
        public ImmutableDictionary<VariableLocation, object> Variables { get; }

        public InterprativeScope(ImmutableDictionary<VariableLocation, object> vars) {
            this.Variables = vars;
        }

        public InterprativeScope() {
            this.Variables = ImmutableDictionary<VariableLocation, object>.Empty;
        }

        public InterprativeScope AddVariable(VariableLocation location, object value) {
            return new InterprativeScope(this.Variables.Add(location, value));
        }
    }
}