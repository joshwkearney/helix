using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Variables;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Variables;

public record AssignmentStatement : IParseStatement {
    public required TokenLocation Location { get; init; }
        
    public required IParseExpression Left { get; init; }
        
    public required IParseExpression Right { get; init; }

    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        (var left, types) = this.Left.CheckTypes(types);
        (var right, types) = this.Right.CheckTypes(types);
            
        // We have to be able to write into the left hand side
        var lValue = left.ToLValue(types);
        var assignSignature = lValue.ReturnType.GetSignature(types);

        HelixType assignType;
        
        if (right.CanPunTo(assignSignature, types)) {
            assignType = right.ReturnType;
        }
        else {
            assignType = assignSignature;
            right = right.UnifyTo(assignSignature, types);
        }
        
        // If we're assigning a local variable, we need to update our stored value
        if (lValue.ReturnType is NominalType nom) {
            types = types.WithRefinement(nom.Path, assignType);
        }
        
        var result = new TypedAssignmentStatement {
            Location = this.Location,
            Left = lValue,
            Right = right,
        };

        return new(result, types);
    }
}