using Helix.Parsing;
using Helix.Syntax.TypedTree.Functions;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Functions {
    public record ReturnParseTree : IParseTree {
        public required TokenLocation Location { get; init; }
        
        public required IParseTree Operand { get; init; }
        
        public bool IsPure => false;

        public TypeCheckResult CheckTypes(TypeFrame types) {
            if (!this.TryGetCurrentFunction(types, out var sig)) {
                throw new InvalidOperationException();
            }

            (var operand, types) = this.Operand.CheckTypes(types);
            operand = operand.UnifyTo(sig.ReturnType, types);
            
            var result = new ReturnTypedTree {
                Location = this.Location,
                Operand = operand,
                FunctionSignature = sig
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
