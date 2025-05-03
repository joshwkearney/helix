using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Variables;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Variables;

public record DereferenceExpression : IParseExpression {
    public required TokenLocation Location { get; init; }
        
    public required IParseExpression Operand { get; init; }
    
    public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
        (var operand, types) = this.Operand.CheckTypes(types);
            
        if (operand.ReturnType.AsReference(types).TryGetValue(out var refType) && refType.InnerType is NominalType nom && nom.Kind == NominalTypeKind.Variable) {
            if (types.TryGetVariable(nom.Path, out var refinement)) {
                var access = new TypedVariableAccessExpression {
                    Location = this.Location,
                    VariablePath = nom.Path,
                    ReturnType = refinement
                };

                return new TypeCheckResult<ITypedExpression>(access, types);
            }
        }

        var pointerType = operand.AssertIsReference(types);

        var result = new TypedDereferenceExpression {
            Location = this.Location,
            Operand = operand,
            OperandSignature = pointerType
        };

        return new TypeCheckResult<ITypedExpression>(result, types);
    }
}