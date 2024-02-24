using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;

namespace Helix.Features.Types {
    public record FunctionType : HelixType {
        public HelixType ReturnType { get; }

        public IReadOnlyList<FunctionParameter> Parameters { get; }

        public FunctionType(HelixType returnType, IReadOnlyList<FunctionParameter> pars) {
            this.ReturnType = returnType;
            this.Parameters = pars;
        }

        public override PassingSemantics GetSemantics(TypeFrame types) {
            return PassingSemantics.ReferenceType;
        }

        public override HelixType GetMutationSupertype(TypeFrame types) => this;

        public override HelixType GetSignature(TypeFrame types) => this;
    }

    public record FunctionParameter {
        public string Name { get; }

        public HelixType Type { get; }

        public FunctionParameter(string name, HelixType type) {
            this.Name = name;
            this.Type = type;
        }
    }
}