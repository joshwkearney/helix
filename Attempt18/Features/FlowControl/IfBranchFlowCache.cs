using System;
namespace Attempt18.Features.FlowControl {
    public class IfBranchFlowCache : IFlowCache {
        private readonly IFlowCache original;
        private readonly IFlowCache copy;

        public IfBranchFlowCache(IFlowCache original, IFlowCache copy) {
            this.original = original;
            this.copy = copy;
        }

        public IFlowCache Clone() {
            return new IfBranchFlowCache(this.original, this.copy.Clone());
        }

        public IdentifierPath[] GetAncestorVariables(IdentifierPath dependentVariable) {
            return this.copy.GetAncestorVariables(dependentVariable);
        }

        public IdentifierPath[] GetDependentVariables(IdentifierPath ancestorVariable) {
            return this.copy.GetDependentVariables(ancestorVariable);
        }

        public bool IsVariableMoved(IdentifierPath variable) {
            return this.copy.IsVariableMoved(variable);
        }

        public void RegisterDependency(IdentifierPath dependentVariable, IdentifierPath ancestorVariable) {
            this.copy.RegisterDependency(dependentVariable, ancestorVariable);
            this.original.RegisterDependency(dependentVariable, ancestorVariable);
        }

        public void SetVariableMoved(IdentifierPath variable, bool moved) {
            this.copy.SetVariableMoved(variable, moved);
            this.original.SetVariableMoved(variable, moved);
        }
    }
}
