using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Variables;

public record AssignmentParseStatement : IParseSyntax {
    public required TokenLocation Location { get; init; }
        
    public required IParseSyntax Operand { get; init; }
        
    public required IParseSyntax Assignment { get; init; }
        
    public bool IsPure => false;

    public ISyntax CheckTypes(TypeFrame types) {
        var operand = this.Operand.CheckTypes(types).ToLValue(types);

        var varSig = operand.ReturnType
            .AsVariable(types)
            .GetValue()
            .InnerType
            .GetMutationSupertype(types);

        var assign = this.Assignment
            .CheckTypes(types)
            .ToRValue(types);
            
        assign = assign.UnifyTo(varSig, types);

        var result = new AssignmentStatement {
            Location = this.Location,
            Operand = operand,
            Assignment = assign
        };

        return result;
    }
}