using Attempt19.Types;

namespace Attempt19.TypeChecking {
    public enum VariableDefinitionKind {
        Local, Alias
    }

    public class VariableInfo {
        public VariableDefinitionKind DefinitionKind { get; }

        public LanguageType Type { get; }

        public VariableInfo(LanguageType innerType, VariableDefinitionKind alias) {
            this.Type = innerType;
            this.DefinitionKind = alias;
        }
    }
}