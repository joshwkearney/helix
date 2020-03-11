using Attempt17.TypeChecking;
using Attempt17.Types;
using System;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.CodeGeneration {
    public interface ICScope : IScope {
        public void SetVariableUndestructed(string name, LanguageType type);

        public void SetVariableDestructed(string name);

        public void SetVariableMoved(string name);

        public ImmutableDictionary<string, LanguageType> GetUndestructedVariables();
    }
}