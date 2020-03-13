using Attempt17.Types;

namespace Attempt17 {
    public enum VariableDefinitionKind {
        Local, Alias
    }

    public class VariableInfo : IIdentifierTarget {
        public VariableDefinitionKind DefinitionKind { get; }

        public LanguageType Type { get; }

        public IdentifierPath Path { get; }

        public bool IsFunctionParameter { get; }

        public VariableInfo(LanguageType type, VariableDefinitionKind kind, IdentifierPath path, bool isFuncParameter = false) {
            this.Type = type;
            this.DefinitionKind = kind;
            this.Path = path;
            this.IsFunctionParameter = isFuncParameter;
        }

        public T Accept<T>(IIdentifierTargetVisitor<T> visitor) {
            return visitor.VisitVariable(this);
        }
    }
}