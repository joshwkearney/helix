using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Parsing;
using Helix.Features.Types;
using Helix.Analysis;

namespace Helix.Features.Functions {
    public record FunctionParseSignature {
        public IParseTree ReturnType { get; }

        public string Name { get; }

        public IReadOnlyList<ParseFunctionParameter> Parameters { get; }

        public TokenLocation Location { get; }

        public FunctionParseSignature(TokenLocation loc, string name, IParseTree returnType, IReadOnlyList<ParseFunctionParameter> pars) {
            this.Location = loc;
            this.ReturnType = returnType;
            this.Name = name;
            this.Parameters = pars;
        }

        public FunctionType ResolveNames(TypeFrame types) {
            var pars = new List<FunctionParameter>();

            if (!this.ReturnType.AsType(types).TryGetValue(out var retType)) {
                throw TypeException.ExpectedTypeExpression(this.ReturnType.Location);
            }

            foreach (var par in this.Parameters) {
                if (!par.Type.AsType(types).TryGetValue(out var parType)) {
                    throw TypeException.ExpectedTypeExpression(par.Location);
                }

                pars.Add(new FunctionParameter(par.Name, parType));
            }

            return new FunctionType(retType, pars);
        }
    }

    public record ParseFunctionParameter {
        public string Name { get; }

        public IParseTree Type { get; }

        public TokenLocation Location { get; }

        public ParseFunctionParameter(TokenLocation loc, string name, IParseTree type) {
            this.Location = loc;
            this.Name = name;
            this.Type = type;
        }
    }
}