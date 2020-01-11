using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public class VariableInfo {
        public VariableDefinitionKind DefinitionKind { get; }

        public LanguageType Type { get; }

        public IdentifierPath Path { get; }

        public VariableInfo(LanguageType type, VariableDefinitionKind kind, IdentifierPath path) {
            this.Type = type;
            this.DefinitionKind = kind;
            this.Path = path;
        }
    }
}