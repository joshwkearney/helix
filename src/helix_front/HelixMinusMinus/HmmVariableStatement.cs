using Helix.Parsing;

namespace Helix.HelixMinusMinus {
    public record HmmVariableStatement : IHmmStatement {
        public HmmVariable Variable { get; init; }

        public TokenLocation Location { get; init; }

        public string[] Write() {
            return new[] { $"var {this.Variable.Name} as {this.Variable.Type};" };
        }
    }
}
