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

    public ISyntax CheckTypes(TypeFrame types) {
        var operand = this.Left.CheckTypes(types).ToLValue(types);

        var varSig = operand.ReturnType
            .AsVariable(types)
            .GetValue()
            .InnerType
            .GetMutationSupertype(types);

        var assign = this.Right
            .CheckTypes(types)
            .ToRValue(types);
            
        assign = assign.UnifyTo(varSig, types);

        var result = new AssignmentStatement {
            Location = this.Location,
            Left = operand,
            Right = assign
        };

        return result;
    }
}