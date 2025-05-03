using Helix.TypeChecking;

namespace Helix.Types;

public record FunctionSignature {
    public HelixType ReturnType { get; }

    public IReadOnlyList<FunctionParameter> Parameters { get; }

    public FunctionSignature(HelixType returnType, IReadOnlyList<FunctionParameter> pars) {
        this.ReturnType = returnType;
        this.Parameters = pars;
    }
}

public record FunctionParameter {
    public string Name { get; }

    public HelixType Type { get; }

    public FunctionParameter(string name, HelixType type) {
        this.Name = name;
        this.Type = type;
    }
}