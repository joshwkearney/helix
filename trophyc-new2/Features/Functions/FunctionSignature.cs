using Trophy.Analysis;
using Trophy.Analysis.Types;

namespace Trophy.Features.Functions {
    public record FunctionSignature  {
        public TrophyType ReturnType { get; }

        public IReadOnlyList<FunctionParameter> Parameters { get; }

        public IdentifierPath Path { get; }

        public FunctionSignature(IdentifierPath path, TrophyType returnType, IReadOnlyList<FunctionParameter> pars) {
            this.Path = path;
            this.ReturnType = returnType;
            this.Parameters = pars;
        }
    }

    public record FunctionParameter {
        public string Name { get; }

        public TrophyType Type { get; }

        public bool IsWritable { get; }

        public FunctionParameter(string name, TrophyType type, bool isWritable) {
            this.Name = name;
            this.Type = type;
            this.IsWritable = isWritable;
        }
    }
}