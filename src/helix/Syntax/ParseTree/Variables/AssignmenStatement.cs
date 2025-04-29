using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Variables;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Variables;

public record AssignmenStatement : IParseStatement {
    public required TokenLocation Location { get; init; }
        
    public required IParseExpression Left { get; init; }
        
    public required IParseExpression Right { get; init; }
        
    public bool IsPure => false;

    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        (var left, types) = this.Left.CheckTypes(types);
        (var right, types) = this.Right.CheckTypes(types);
            
        // We have to be able to write into the left hand side
        var lValue = left.ToLValue(types);
        
        var innerSignature = lValue.ReturnType
            .AsVariable(types)
            .GetValue()
            .InnerType
            .GetSignature(types);

        HelixType returnType;
        
        if (right.CanPunTo(innerSignature, types)) {
            returnType = right.ReturnType;
        }
        else {
            returnType = innerSignature;
            right = right.UnifyTo(innerSignature, types);
        }
        
        // If we're assigning a local variable, we need to update our stored value
        if (lValue.ReturnType is NominalType nom) {
            types = types.WithRefinement(nom.Path, new PointerType(returnType));
        }
        
        var result = new TypedAssignmentStatement {
            Location = this.Location,
            Left = lValue,
            Right = right,
        };

        return new(result, types);
    }
}