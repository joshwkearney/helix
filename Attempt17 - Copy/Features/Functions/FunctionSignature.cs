using Attempt17.Types;
using System.Collections.Immutable;

namespace Attempt17.Features.Functions {
    public class FunctionSignature {
        public LanguageType ReturnType { get; }

        public ImmutableList<FunctionParameter> Parameters { get; }

        public string Name { get; }

        public FunctionSignature(string name, LanguageType returnType, ImmutableList<FunctionParameter> pars) {
            this.Name = name;
            this.ReturnType = returnType;
            this.Parameters = pars;
        }
    }
}