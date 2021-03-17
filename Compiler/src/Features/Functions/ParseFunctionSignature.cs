using System.Collections.Generic;

namespace Trophy.Features.Functions {
    public class ParseFunctionSignature {
        public ISyntaxA ReturnType { get; }

        public string Name { get; }

        public IReadOnlyList<ParseFunctionParameter> Parameters { get; }

        public ParseFunctionSignature(string name, ISyntaxA returnType, IReadOnlyList<ParseFunctionParameter> pars) {
            this.ReturnType = returnType;
            this.Name = name;
            this.Parameters = pars;
        }
    }

    public class ParseFunctionParameter {
        public string Name { get; }

        public ISyntaxA Type { get; }

        public ParseFunctionParameter(string name, ISyntaxA type) {
            this.Name = name;
            this.Type = type;
        }
    }
}