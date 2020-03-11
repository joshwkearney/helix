using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Attempt17.Features.FlowControl {
    public class BlockCScope : ICScope {
        private readonly ICScope head;
        private readonly Dictionary<string, LanguageType> undestructedVariables = new Dictionary<string, LanguageType>();

        public BlockCScope(ICScope head) {
            this.head = head;
        }

        public IOption<TypeInfo> FindTypeInfo(IdentifierPath path) {
            return this.head.FindTypeInfo(path);
        }

        public ImmutableDictionary<string, LanguageType> GetUndestructedVariables() {
            return this.undestructedVariables.ToImmutableDictionary();
        }

        public void SetVariableDestructed(string name) {
            this.undestructedVariables.Remove(name);
        }

        public void SetVariableMoved(string name) {
            this.undestructedVariables.Remove(name);
            this.head.SetVariableMoved(name);
        }

        public void SetVariableUndestructed(string name, LanguageType type) {
            this.undestructedVariables[name] = type;
        }
    }
}