using Helix.Parsing;
using Helix.Syntax.TypedTree.Functions;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Functions;

public record InvokeParseTree : IParseTree {
    public required TokenLocation Location { get; init; }
        
    public required IParseTree Operand { get; init; }
        
    public required IReadOnlyList<IParseTree> Arguments { get; init; }
        
    public bool IsPure => false;
        
    public TypeCheckResult CheckTypes(TypeFrame types) {
        (var operand, types) = this.Operand.CheckTypes(types);

        // TODO: Support invoking non-nominal functions
        // Make sure the target is a function
        if (!operand.ReturnType.AsFunction(types).TryGetValue(out var sig) || operand.ReturnType is not NominalType named) {
            throw TypeException.ExpectedFunctionType(this.Operand.Location, operand.ReturnType);
        }

        // Make sure the arg count lines up
        if (this.Arguments.Count != sig.Parameters.Count) {
            throw TypeException.ParameterCountMismatch(
                this.Location, 
                sig.Parameters.Count, 
                this.Arguments.Count);
        }

        var newArgs = new ITypedTree[this.Arguments.Count];

        // Make sure the arg types line up
        for (int i = 0; i < this.Arguments.Count; i++) {
            var expectedType = sig.Parameters[i].Type;

            (newArgs[i], types) = this.Arguments[i].CheckTypes(types);
            newArgs[i] = newArgs[i].UnifyTo(expectedType, types);
        }

        var result = new InvokeTypedTree {
            Location = this.Location,
            FunctionSignature = sig,
            FunctionPath = named.Path,
            Arguments = newArgs,
            AlwaysJumps = newArgs.Any(x => x.AlwaysJumps)
        };

        return new TypeCheckResult(result, types);            
    }
}