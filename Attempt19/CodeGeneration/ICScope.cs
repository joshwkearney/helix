using Attempt18.TypeChecking;
using Attempt18.Types;
using System;
using System.Collections.Immutable;
using System.Text;

namespace Attempt18.CodeGeneration {
    public interface ICScope {
        public void SetVariableUndestructed(string name, LanguageType type);

        public void SetVariableDestructed(string name);

        public ImmutableDictionary<string, LanguageType> GetUndestructedVariables();
    }
}