using Attempt6.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt6.Analyzing {
    public class Scope {
        public IReadOnlyDictionary<string, VariableLocation> Variables { get; }

        public Scope() {
            this.Variables = new Dictionary<string, VariableLocation>();
        }

        public Scope(IReadOnlyDictionary<string, VariableLocation> variables) {
            this.Variables = variables;
        }

        public Scope AppendVariable(string name, VariableLocation location) {
            var dict = this.Variables.ToDictionary(x => x.Key, x => x.Value);
            dict.Add(name, location);

            return new Scope(dict);
        }
    }
}