using Attempt19.CodeGeneration;
using Attempt19.Types;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Attempt19.Features.FlowControl {
    public class BlockCScope : ICScope {
        private readonly ICScope head;
        private readonly Dictionary<string, LanguageType> undestructedVariables = new Dictionary<string, LanguageType>();

        public BlockCScope(ICScope head) {
            this.head = head;
        }

        public ImmutableDictionary<string, LanguageType> GetUndestructedVariables() {
            return this.undestructedVariables.ToImmutableDictionary();
        }

        public void SetVariableDestructed(string name) {
            this.undestructedVariables.Remove(name);
        }

        public void SetVariableUndestructed(string name, LanguageType type) {
            this.undestructedVariables[name] = type;
        }
    }
}