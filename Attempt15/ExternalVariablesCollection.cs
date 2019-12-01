using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace JoshuaKearney.Attempt15 {
    public class ExternalVariablesCollection {
        private readonly ImmutableDictionary<string, VariableInfo> vars;
        private readonly ImmutableHashSet<string> allocatedVars;

        public ExternalVariablesCollection() {
            this.allocatedVars = ImmutableHashSet<string>.Empty;
            this.vars = ImmutableDictionary<string, VariableInfo>.Empty;
        }

        private ExternalVariablesCollection(ImmutableDictionary<string, VariableInfo> dict, ImmutableHashSet<string> allocatedVars) {
            this.allocatedVars = allocatedVars;
            this.vars = dict;
        }

        public IEnumerable<string> VariableNames => this.vars.Keys;

        public IEnumerable<VariableInfo> VariableInfos => this.vars.Values;

        public IEnumerable<string> AllocatedVariables => this.allocatedVars;

        public ExternalVariablesCollection SetVariableAllocated(string name, bool isAllocated) {
            if (isAllocated) {
                return new ExternalVariablesCollection(this.vars, this.allocatedVars.Add(name));
            }
            else {
                return new ExternalVariablesCollection(this.vars, this.allocatedVars.Remove(name));
            }
        }

        public ExternalVariablesCollection SetVariableInfo(VariableInfo info) {
            return new ExternalVariablesCollection(this.vars.SetItem(info.Name, info), this.allocatedVars);
        }

        public VariableInfo GetVariableInfo(string var) {
            return this.vars[var];
        }

        public ExternalVariablesCollection Union(ExternalVariablesCollection other) {
            var vars = this.vars;
            var allocated = this.allocatedVars.Union(other.allocatedVars);

            foreach (var item in other.vars) {
                if (vars.TryGetValue(item.Key, out var value)) {
                    if (!value.Equals(item.Value)) {
                        throw new Exception();
                    }
                }
                else {
                    vars = vars.Add(item.Key, item.Value);
                }
            }

            return new ExternalVariablesCollection(vars, allocated);
        }

        public ExternalVariablesCollection Except(IEnumerable<string> names) {
            var vars = this.vars;
            var allocated = this.allocatedVars;

            foreach (var name in names) {
                vars = vars.Remove(name);
                allocated = allocated.Remove(name);
            }

            return new ExternalVariablesCollection(vars, allocated);
        }

        public ExternalVariablesCollection Except(string name) {
            return new ExternalVariablesCollection(
                this.vars.Remove(name),
                this.allocatedVars.Remove(name)
            );
        }
    }
}
