namespace Helix.Common.Types {
    public record FunctionSignature {
        public required IHelixType ReturnType { get; init; }

        public required IReadOnlyList<FunctionParameter> Parameters { get; init; }
    }

    public record FunctionParameter {
        public required string Name { get; init; }

        public required IHelixType Type { get; init; }

        public required bool IsMutable { get; init; }

        public override string ToString() {
            return (this.IsMutable ? "var" : "let") + " " + this.Name + " as " + this.Type.ToString();
        }
    }
}