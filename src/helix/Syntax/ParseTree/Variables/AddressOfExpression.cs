using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Variables;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Variables;

public class AddressOfExpression : IParseExpression {
    public required TokenLocation Location { get; init; }
        
    public required IParseExpression Operand { get; init; }
    
    public Option<HelixType> AsType(TypeFrame types) {
        return this.Operand
            .AsType(types)
            .Select(HelixType (x) => new PointerType(x));
    }

    public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
        (var operand, types) = this.Operand.CheckTypes(types);

        var lvalue = operand.ToLValue(types);
        
        // Make sure we're taking the address of a local variable
        if (lvalue is not ILValue.Local local) {
            throw TypeException.ExpectedVariableType(this.Operand.Location, operand.ReturnType);
        }
        
        // We need to flush this variable's signature because its value can now be set through an alias
        types = types.PopValue(local.VariablePath);

        var result = new TypedAddressOfExpression {
            Location = this.Location,
            ReturnType = local.ReturnType,
            VariablePath = local.VariablePath
        };

        return new TypeCheckResult<ITypedExpression>(result, types);
    }
}