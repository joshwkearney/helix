using Attempt19.Types;

namespace Attempt19 {
    public enum VariableDefinitionKind {
        Local, Alias
    }

    public class VariableInfo {
        private readonly LanguageType innerType;
        private readonly VariableDefinitionKind alias;

        public VariableInfo(LanguageType innerType, VariableDefinitionKind alias) {
            this.innerType = innerType;
            this.alias = alias;
        }

        public VariableDefinitionKind DefinitionKind => this.alias;

        public LanguageType Type => this.innerType;
    }
}