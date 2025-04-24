using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Parsing;
using Helix.Features.Types;

namespace Helix.Features.Functions {
    public record ReturnParseSyntax : IParseSyntax {
        public required TokenLocation Location { get; init; }
        
        public required IParseSyntax Operand { get; init; }
        
        public bool IsPure => false;

        public ISyntax CheckTypes(TypeFrame types) {
            if (!this.TryGetCurrentFunction(types, out var sig)) {
                throw new InvalidOperationException();
            }

            var operand = this.Operand
                .CheckTypes(types)
                .ToRValue(types)
                .UnifyTo(sig.ReturnType, types);

            var result = new ReturnSyntax {
                Location = this.Location,
                Operand = operand
            };

            return result;
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
