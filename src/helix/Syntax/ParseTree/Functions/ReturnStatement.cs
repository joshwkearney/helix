using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Functions;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Functions;

public record ReturnStatement : IParseStatement {
    public required TokenLocation Location { get; init; }
        
    public required IParseExpression Operand { get; init; }
        
    public bool IsPure => false;

    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        if (!this.TryGetCurrentFunction(types, out var sig)) {
            throw new InvalidOperationException();
        }

        (var operand, types) = this.Operand.CheckTypes(types);
        operand = operand.UnifyTo(sig.ReturnType, types);
            
        var result = new TypedReturnStatement {
            Location = this.Location,
            Operand = operand,
            FunctionSignature = sig
        };

        return new(result, types);
    }
        
    private bool TryGetCurrentFunction(TypeFrame types, out FunctionSignature func) {
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