using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Variables;

public record AssignmentParseStatement : IParseSyntax {
    public required TokenLocation Location { get; init; }
        
    public required IParseSyntax Left { get; init; }
        
    public required IParseSyntax Right { get; init; }
        
    public bool IsPure => false;

    public TypeCheckResult CheckTypes(TypeFrame types) {
        (var left, types) = this.Left.CheckTypes(types);
        (var right, types) = this.Right.CheckTypes(types);
            
        // We have to be able to write into the left hand side
        left = left.ToLValue(types);

        var innerType = left.ReturnType
            .AsVariable(types)
            .GetValue()
            .InnerType
            .GetSignature(types);
        
        // The assigned type needs to match the left hand side
        right = right.UnifyTo(innerType, types);
        
        // If we're assigning a local variable, we need to flush the type
        // signature so it's not out of date with this assignment
        
        // TODO: Instead of writing the signature type, write the assigned type if
        // this variable hasn't had its address taken
        if (left.ReturnType is NominalType nom) {
            types = types.WithNominalSignature(nom.Path, new PointerType(innerType));
        }
        
        var result = new AssignmentStatement {
            Location = this.Location,
            Left = left,
            Right = right,
            AlwaysJumps = left.AlwaysJumps || right.AlwaysJumps
        };

        return new TypeCheckResult(result, types);
    }
}