using Attempt19.Types;
using System.Collections.Immutable;

namespace Attempt19.CodeGeneration {
    public interface ICScope {
        public void SetVariableUndestructed(string name, LanguageType type);

        public void SetVariableDestructed(string name);

        public ImmutableDictionary<string, LanguageType> GetUndestructedVariables();
    }
}