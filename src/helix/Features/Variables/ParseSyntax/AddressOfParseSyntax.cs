using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
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
        
        // Make sure we're taking the address of a local variable
        if (operand.ReturnType is not NominalType nominal || nominal.Kind != NominalTypeKind.Variable) {
            throw new InvalidOperationException();
        }
        
        // We need to flush this variable's signature because its value can now be set through an alias
        types = types.WithNominalSignature(nominal.Path, nominal.GetSignature(types));

        var result = new AddressOfSyntax {
            Location = this.Location,
            Operand = operand,
            ReturnType = nominal
        };

        return new TypeCheckResult(result, types);
    }
}