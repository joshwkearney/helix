using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.Functions {
    public class FunctionCScope : ICScope {
        private readonly Dictionary<string, LanguageType> undestructedVariables = new Dictionary<string, LanguageType>();

        public ImmutableDictionary<string, LanguageType> GetUndestructedVariables() {
            return this.undestructedVariables.ToImmutableDictionary();
        }

        public void SetVariableDestructed(string name) {
            this.undestructedVariables.Remove(name);
        }

        public void SetVariableMoved(string name) {
            this.undestructedVariables.Remove(name);
        }

        public void SetVariableUndestructed(string name, LanguageType type) {
            this.undestructedVariables.Add(name, type);
        }
    }
}