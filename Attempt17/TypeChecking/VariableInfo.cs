using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public class VariableInfo {
        public VariableSource Source { get; }

        public LanguageType Type { get; }

        public IdentifierPath Path { get; }

        public VariableInfo(LanguageType type, VariableSource source, IdentifierPath path) {
            this.Type = type;
            this.Source = source;
            this.Path = path;
        }
    }
}