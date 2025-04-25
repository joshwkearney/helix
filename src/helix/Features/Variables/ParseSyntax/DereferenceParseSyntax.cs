using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Variables;

public record DereferenceParseSyntax : IParseSyntax {
    public required TokenLocation Location { get; init; }
        
    public required IParseSyntax Operand { get; init; }
        
    public bool IsPure => this.Operand.IsPure;

    public Option<HelixType> AsType(TypeFrame types) {
        return this.Operand.AsType(types)
            .Select(x => new PointerType(x))
            .Select(x => (HelixType)x);
    }

    public TypeCheckResult CheckTypes(TypeFrame types) {
        (var operand, types) = this.Operand.CheckTypes(types);
            
        if (operand.ReturnType is NominalType nom && nom.Kind == NominalTypeKind.Variable) {
            var sig = nom.AsVariable(types).GetValue();

            var access = new VariableAccessSyntax {
                Location = this.Location,
                VariablePath = nom.Path,
                VariableSignature = sig,
                IsLValue = false
            };

            return new TypeCheckResult(access, types);
        }

        var pointerType = operand.AssertIsPointer(types);

        var result = new DereferenceSyntax {
            Location = this.Location,
            Operand = operand,
            OperandSignature = pointerType,
            IsLValue = false
        };

        return new TypeCheckResult(result, types);
    }
}