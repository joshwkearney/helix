using Helix.Analysis;
using Helix.Analysis.Types;

namespace Helix.Features.Functions {
    public record FunctionSignature  {
        public HelixType ReturnType { get; }

        public IReadOnlyList<FunctionParameter> Parameters { get; }

        public IdentifierPath Path { get; }

        public FunctionSignature(IdentifierPath path, HelixType returnType, IReadOnlyList<FunctionParameter> pars) {
            this.Path = path;
            this.ReturnType = returnType;
            this.Parameters = pars;
        }
    }

    public record FunctionParameter {
        public string Name { get; }

        public HelixType Type { get; }

        public bool IsWritable { get; }

        public FunctionParameter(string name, HelixType type, bool isWritable) {
            this.Name = name;
            this.Type = type;
            this.IsWritable = isWritable;
        }
    }
}