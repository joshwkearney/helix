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
        (var operand, types) = this.Left.CheckTypes(types);
        (var assign, types) = this.Right.CheckTypes(types);
            
        // We have to be able to write into the left hand side
        operand = operand.ToLValue(types);

        var varType = operand.ReturnType
            .AsVariable(types)
            .GetValue()
            .InnerType
            .GetSignature(types);
        
        // The assigned type needs to match the left hand side
        assign = assign.UnifyTo(varType, types);
        
        var result = new AssignmentStatement {
            Location = this.Location,
            Left = operand,
            Right = assign
        };

        return new TypeCheckResult(result, types);
    }
}