using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree {
    public record ParseFunctionSignature {
        public required TokenLocation Location { get; init; }
        
        public required string Name { get; init; }

        public required IParseExpression ReturnType { get; init; }
        
        public required IReadOnlyList<ParseFunctionParameter> Parameters { get; init; }

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
}