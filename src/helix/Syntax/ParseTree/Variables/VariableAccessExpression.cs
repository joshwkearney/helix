using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Functions;
using Helix.Syntax.TypedTree.Variables;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Variables;

public record VariableAccessExpression : IParseExpression {
    public required TokenLocation Location { get; init; }
        
    public required string VariableName { get; init; }

    public Option<HelixType> AsType(TypeFrame types) {
        // If we're pointing at a type then return it
        if (types.TryResolveName(types.Scope, this.VariableName, out var type)) {
            return type;
        }

        return Option.None;
    }

    public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
        // Make sure this name exists
        if (!types.TryResolvePath(types.Scope, this.VariableName, out var path)) {
            throw TypeException.VariableUndefined(this.Location, this.VariableName);
        }

        // See if we are accessing a variable
        if (types.TryGetVariable(path, out var refinement)) {
            var result = new TypedVariableAccessExpression {
                Location = this.Location,
                VariablePath = path,
                ReturnType = refinement
            };

            return new TypeCheckResult<ITypedExpression>(result, types);
        }

        // See if we are accessing a function
        if (types.TryGetFunction(path, out var funcSig)) {
            var result = new TypedFunctionAccessExpression {
                Location = this.Location,
                FunctionPath = path
            };

            return new TypeCheckResult<ITypedExpression>(result, types);
        }

        throw TypeException.VariableUndefined(this.Location, this.VariableName);
    }
}