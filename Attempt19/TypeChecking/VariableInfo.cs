using Attempt19.Types;
using System.Collections.Immutable;

namespace Attempt19.TypeChecking {
    public enum VariableDefinitionKind {
        Local, Alias
    }

    public class VariableInfo {
        public VariableDefinitionKind DefinitionKind { get; }

        public LanguageType Type { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public VariableInfo(LanguageType innerType, VariableDefinitionKind alias, ImmutableHashSet<IdentifierPath> lifetime) {
            this.Type = innerType;
            this.DefinitionKind = alias;
            this.Lifetimes = lifetime;
        }
    }
}