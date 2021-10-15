using Trophy.Analysis.Types;
using System.Collections.Immutable;

namespace Trophy.Analysis {
    public enum VariableSource {
        Local, Parameter
    }

    public enum VariableKind {
        Value, RefVariable, VarVariable
    }

    public class VariableInfo {
        public string Name { get; }

        public ITrophyType Type { get; }

        public int UniqueId { get; }

        public VariableKind Kind { get; }

        public VariableSource Source { get; }

        public VariableInfo(
            string name,
            ITrophyType innerType,
            VariableKind kind,
            VariableSource source,
            int id) {

            this.Type = innerType;
            this.Kind = kind;
            this.Source = source;
            this.UniqueId = id;
            this.Name = name;
        }
    }
}