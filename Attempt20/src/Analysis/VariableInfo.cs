using Attempt20.Analysis.Types;
using System.Collections.Immutable;

namespace Attempt20.Analysis {
    public class VariableInfo {
        public VariableDefinitionKind DefinitionKind { get; }

        public TrophyType Type { get; }

        public ImmutableHashSet<IdentifierPath> ValueLifetimes { get; }

        public ImmutableHashSet<IdentifierPath> VariableLifetimes { get; }

        public VariableInfo(
            TrophyType innerType,
            VariableDefinitionKind alias,
            ImmutableHashSet<IdentifierPath> valueLifetimes,
            ImmutableHashSet<IdentifierPath> variableLifetimes) {

            this.Type = innerType;
            this.DefinitionKind = alias;
            this.ValueLifetimes = valueLifetimes;
            this.VariableLifetimes = variableLifetimes;
        }
    }

    public enum VariableDefinitionKind {
        Local, LocalAllocated, Parameter
    }
}