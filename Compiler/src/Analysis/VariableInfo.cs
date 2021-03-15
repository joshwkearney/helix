using Trophy.Analysis.Types;
using System.Collections.Immutable;

namespace Trophy.Analysis {
    public class VariableInfo {
        public string Name { get; }

        public VariableDefinitionKind DefinitionKind { get; }

        public ITrophyType Type { get; }

        public ImmutableHashSet<IdentifierPath> ValueLifetimes { get; }

        public ImmutableHashSet<IdentifierPath> VariableLifetimes { get; }

        public int UniqueId { get; }

        public VariableInfo(
            string name,
            ITrophyType innerType,
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
        LocalVar, LocalRef, Parameter, ParameterVar, ParameterRef
    }
}