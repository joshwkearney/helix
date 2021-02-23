using Attempt20.Analysis.Types;
using System.Collections.Immutable;

namespace Attempt20.Analysis {
    public class VariableInfo {
        public string Name { get; }

        public VariableDefinitionKind DefinitionKind { get; }

        public TrophyType Type { get; }

        public ImmutableHashSet<IdentifierPath> ValueLifetimes { get; }

        public ImmutableHashSet<IdentifierPath> VariableLifetimes { get; }

        public int UniqueId { get; }

        public VariableInfo(
            string name,
            TrophyType innerType,
            VariableDefinitionKind kind,
            int id,
            ImmutableHashSet<IdentifierPath> valueLifetimes,
            ImmutableHashSet<IdentifierPath> variableLifetimes) {

            this.Type = innerType;
            this.DefinitionKind = kind;
            this.ValueLifetimes = valueLifetimes;
            this.VariableLifetimes = variableLifetimes;
            this.UniqueId = id;
            this.Name = name;
        }
    }

    public enum VariableDefinitionKind {
        LocalVar, LocalRef, Parameter
    }
}