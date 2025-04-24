using Helix.Analysis.TypeChecking;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Variables;

public class AddressOfParseSyntax : IParseSyntax {
    public required TokenLocation Location { get; init; }
        
    public required IParseSyntax Operand { get; init; }
        
    public bool IsPure => this.Operand.IsPure;

    public ISyntax CheckTypes(TypeFrame types) {
        var operand = this.Operand.CheckTypes(types).ToLValue(types);
        var varType = operand.ReturnType;

        var result = new AddressOfSyntax {
            Location = this.Location,
            Operand = operand,
            ReturnType = varType
        };

        return result;
    }
}