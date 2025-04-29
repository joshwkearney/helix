using Helix.Parsing;
using Helix.Syntax.TypedTree.Variables;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Variables;

public record DereferenceParseTree : IParseTree {
    public required TokenLocation Location { get; init; }
        
    public required IParseTree Operand { get; init; }
    
    public TypeCheckResult<ITypedTree> CheckTypes(TypeFrame types) {
        (var operand, types) = this.Operand.CheckTypes(types);
            
        if (operand.ReturnType is NominalType nom && nom.Kind == NominalTypeKind.Variable) {
            var sig = nom.AsVariable(types).GetValue();

            var access = new VariableAccessTypedTree {
                Location = this.Location,
                VariablePath = nom.Path,
                ReturnType = sig.InnerType
            };

            return new TypeCheckResult<ITypedTree>(access, types);
        }

        var pointerType = operand.AssertIsPointer(types);

        var result = new DereferenceTypedTree {
            Location = this.Location,
            Operand = operand,
            OperandSignature = pointerType
        };

        return new TypeCheckResult<ITypedTree>(result, types);
    }
}