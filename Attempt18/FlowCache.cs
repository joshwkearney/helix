using System;
using System.Collections.Generic;
using System.Linq;

namespace Attempt19 {
    public class FlowCache : IFlowCache {
        private readonly HashSet<IdentifierPath> movedVariables;
        private readonly Dictionary<IdentifierPath, List<IdentifierPath>> dependents;
        private readonly Dictionary<IdentifierPath, List<IdentifierPath>> ancestors;

        public FlowCache() {
            this.movedVariables = new HashSet<IdentifierPath>();
            this.dependents = new Dictionary<IdentifierPath, List<IdentifierPath>>();
            this.ancestors = new Dictionary<IdentifierPath, List<IdentifierPath>>();
        }

        private FlowCache(HashSet<IdentifierPath> movedVars,
            Dictionary<IdentifierPath, List<IdentifierPath>> deps,
            Dictionary<IdentifierPath, List<IdentifierPath>> ancs) {

            this.movedVariables = movedVars;
            this.dependents = deps;
            this.ancestors = ancs;
        }

        public void SetVariableMoved(IdentifierPath variable, bool moved) {
            if (moved) {
                this.movedVariables.Add(variable);
            }
            else {
                this.movedVariables.Remove(variable);
            }
        }

        public bool IsVariableMoved(IdentifierPath variable) {
            return this.movedVariables.Contains(variable);
        }

        public void RegisterDependency(IdentifierPath dependentVariable, IdentifierPath ancestorVariable) {
            if (!this.dependents.TryGetValue(ancestorVariable, out var dependentsList)) {
                this.dependents[ancestorVariable] = dependentsList = new List<IdentifierPath>();
            }

            if (!this.ancestors.TryGetValue(dependentVariable, out var ancestorList)) {
                this.ancestors[dependentVariable] = ancestorList = new List<IdentifierPath>();
            }

            dependentsList.Add(dependentVariable);
            ancestorList.Add(ancestorVariable);
        }

        public IdentifierPath[] GetDependentVariables(IdentifierPath ancestorVariable) {
            var visited = new HashSet<IdentifierPath>();
            var toVisit = new Stack<IdentifierPath>();

            toVisit.Push(ancestorVariable);

            while (toVisit.Count > 0) {
                var node = toVisit.Pop();

                visited.Add(node);

                if (this.dependents.TryGetValue(node, out var list)) { 
                    foreach (var neighbor in list) {
                        if (!visited.Contains(neighbor)) {
                            toVisit.Push(neighbor);
                        }
                    }
                }
            }

            return visited.ToArray();
        }

        public IdentifierPath[] GetAncestorVariables(IdentifierPath dependentVariable) {
            var visited = new HashSet<IdentifierPath>();
            var toVisit = new Stack<IdentifierPath>();

            toVisit.Push(dependentVariable);

            while (toVisit.Count > 0) {
                var node = toVisit.Pop();

                visited.Add(node);

                if (this.ancestors.TryGetValue(node, out var list)) {
                    foreach (var neighbor in list) {
                        if (!visited.Contains(neighbor)) {
                            toVisit.Push(neighbor);
                        }
                    }
                }
            }

            return visited.ToArray();
        }

        public IFlowCache Clone() {
            var clone = new FlowCache(
                this.movedVariables.ToHashSet(),
                this.dependents.ToDictionary(x => x.Key, x => x.Value),
                this.ancestors.ToDictionary(x => x.Key, x => x.Value));

            return clone;
        }
    }
}
