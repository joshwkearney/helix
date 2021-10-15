using System.Collections.Generic;
using Trophy.Analysis;

namespace Trophy.Features.Functions {
    public class ParseFunctionSignature {
        public ISyntaxA ReturnType { get; }

        public string Name { get; }

        public IReadOnlyList<ParseFunctionParameter> Parameters { get; }

        public IReadOnlyList<bool> AreParametersWrapped { get; }

        public ParseFunctionSignature(string name, ISyntaxA returnType, IReadOnlyList<ParseFunctionParameter> pars) {
            this.ReturnType = returnType;
            this.Name = name;
            this.Parameters = pars;
        }
    }

    public class ParseFunctionParameter {
        public string Name { get; }

        public ISyntaxA Type { get; }

        public VariableKind Kind { get; }

        public ParseFunctionParameter(string name, ISyntaxA type, VariableKind kind) {
            this.Name = name;
            this.Type = type;
            this.Kind = kind;
        }
    }
}