using Helix.Analysis.TypeChecking;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Variables;

public class AddressOfParseSyntax : IParseSyntax {
    public required TokenLocation Location { get; init; }
        
    public required IParseSyntax Operand { get; init; }
        
    public bool IsPure => this.Operand.IsPure;

    public TypeCheckResult CheckTypes(TypeFrame types) {
        (var operand, types) = this.Operand.CheckTypes(types);
            
        operand = operand.ToLValue(types);
        var varType = operand.ReturnType;

        var result = new AddressOfSyntax {
            Location = this.Location,
            Operand = operand,
            ReturnType = varType
        };

        return new TypeCheckResult(result, types);
    }
}