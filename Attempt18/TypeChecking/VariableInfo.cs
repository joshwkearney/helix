using Attempt19.Types;

namespace Attempt19 {
    public enum VariableDefinitionKind {
        Local, Alias
    }

    public class VariableInfo {
        public VariableDefinitionKind DefinitionKind { get; set; }

        public LanguageType Type { get; set; }

        public bool IsFunctionParameter { get; set; }
    }
}