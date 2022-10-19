using Trophy.Analysis;
using Trophy.Parsing;

namespace Trophy.Features.Functions {
    public record FunctionParseSignature {
        public ISyntax ReturnType { get; }

        public string Name { get; }

        public IReadOnlyList<ParseFunctionParameter> Parameters { get; }

        public TokenLocation Location { get; }

        public FunctionParseSignature(TokenLocation loc, string name, ISyntax returnType, IReadOnlyList<ParseFunctionParameter> pars) {
            this.Location = loc;
            this.ReturnType = returnType;
            this.Name = name;
            this.Parameters = pars;
        }

        public FunctionSignature ResolveNames(ITypesRecorder types) {
            var path = types.CurrentScope.Append(this.Name);
            var pars = new List<FunctionParameter>();

            if (!this.ReturnType.AsType(types).TryGetValue(out var retType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.ReturnType.Location);
            }

            foreach (var par in this.Parameters) {
                if (!par.Type.AsType(types).TryGetValue(out var parType)) {
                    throw TypeCheckingErrors.ExpectedTypeExpression(par.Location);
                }

                pars.Add(new FunctionParameter(par.Name, parType, par.IsWritable));
            }

            return new FunctionSignature(path, retType, pars);
        }
    }

    public record ParseFunctionParameter {
        public string Name { get; }

        public ISyntax Type { get; }

        public bool IsWritable { get; }

        public TokenLocation Location { get; }

        public ParseFunctionParameter(TokenLocation loc, string name, ISyntax type, bool isWritable) {
            this.Location = loc;
            this.Name = name;
            this.Type = type;
            this.IsWritable = isWritable;
        }
    }
}