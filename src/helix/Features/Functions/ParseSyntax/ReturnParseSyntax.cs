using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Functions.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Functions.ParseSyntax {
    public record ReturnParseSyntax : IParseSyntax {
        public required TokenLocation Location { get; init; }
        
        public required IParseSyntax Operand { get; init; }
        
        public bool IsPure => false;

        public TypeCheckResult CheckTypes(TypeFrame types) {
            if (!this.TryGetCurrentFunction(types, out var sig)) {
                throw new InvalidOperationException();
            }

            (var operand, types) = this.Operand.CheckTypes(types);
            operand = operand.UnifyTo(sig.ReturnType, types);
            
            var result = new ReturnSyntax {
                Location = this.Location,
                Operand = operand
            };

            return new TypeCheckResult(result, types);
        }
        
        private bool TryGetCurrentFunction(TypeFrame types, out FunctionType func) {
            var path = types.Scope;

            while (!path.IsEmpty) {
                if (types.TryGetFunction(path, out func)) {
                    return true;
                }

                path = path.Pop();
            }

            func = null;
            return false;
        }
    }
}
