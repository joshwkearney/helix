using Attempt18.Types;

namespace Attempt18 {
    public enum VariableDefinitionKind {
        Local, Alias
    }

    public class VariableInfo {
        public VariableDefinitionKind DefinitionKind { get; set; }

        public LanguageType Type { get; set; }

        public bool IsFunctionParameter { get; set; }
    }
}