using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.FlowControl {
    public class IfBranchCScope : ICScope {
        private readonly ICScope head;

        public ImmutableHashSet<string> MovedVariables { get; private set; } = ImmutableHashSet<string>.Empty;

        public IfBranchCScope(ICScope head) {
            this.head = head;
        }

        public ImmutableDictionary<string, LanguageType> GetUndestructedVariables() {
            return this.head.GetUndestructedVariables();
        }

        public void SetVariableDestructed(string name) {
            this.head.SetVariableDestructed(name);
        }

        public void SetVariableMoved(string name) {
            this.MovedVariables = this.MovedVariables.Add(name);
        }

        public void SetVariableUndestructed(string name, LanguageType type) {
            this.head.SetVariableUndestructed(name, type);
        }

        public IOption<TypeInfo> FindTypeInfo(IdentifierPath path) {
            return this.head.FindTypeInfo(path);
        }
    }
}