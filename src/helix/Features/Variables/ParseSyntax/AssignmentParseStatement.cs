using Helix.Analysis;
using Helix.Analysis.TypeChecking;
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

        var varType = left.ReturnType
            .AsVariable(types)
            .GetValue()
            .InnerType
            .GetSignature(types);
        
        // The assigned type needs to match the left hand side
        right = right.UnifyTo(varType, types);
        
        var result = new AssignmentStatement {
            Location = this.Location,
            Left = left,
            Right = right,
            AlwaysJumps = left.AlwaysJumps || right.AlwaysJumps
        };

        return new TypeCheckResult(result, types);
    }
}