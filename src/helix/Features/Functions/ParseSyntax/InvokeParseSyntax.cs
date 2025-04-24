using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Functions;

public record InvokeParseSyntax : IParseSyntax {
    public required TokenLocation Location { get; init; }
        
    public required IParseSyntax Operand { get; init; }
        
    public required IReadOnlyList<IParseSyntax> Arguments { get; init; }
        
    public bool IsPure => false;
        
    public ISyntax CheckTypes(TypeFrame types) {
        var target = this.Operand.CheckTypes(types).ToRValue(types);
        var targetType = target.ReturnType;

        // TODO: Support invoking non-nominal functions
        // Make sure the target is a function
        if (!targetType.AsFunction(types).TryGetValue(out var sig) || targetType is not NominalType named) {
            throw TypeException.ExpectedFunctionType(this.Operand.Location, targetType);
        }

        // Make sure the arg count lines up
        if (this.Arguments.Count != sig.Parameters.Count) {
            throw TypeException.ParameterCountMismatch(
                this.Location, 
                sig.Parameters.Count, 
                this.Arguments.Count);
        }

        var newArgs = new ISyntax[this.Arguments.Count];

        // Make sure the arg types line up
        for (int i = 0; i < this.Arguments.Count; i++) {
            var expectedType = sig.Parameters[i].Type;

            newArgs[i] = this.Arguments[i].CheckTypes(types).UnifyTo(expectedType, types);
        }

        var result = new InvokeSyntax {
            Location = this.Location,
            FunctionSignature = sig,
            FunctionPath = named.Path,
            Arguments = newArgs
        };

        return result;            
    }
}