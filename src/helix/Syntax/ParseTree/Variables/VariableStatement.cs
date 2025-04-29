using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Variables;

public record VariableStatement : IParseStatement {
    public required TokenLocation Location { get; init; }
        
    public required string VariableName { get; init; }
        
    public required Option<IParseExpression> VariableType { get; init; }
        
    public required IParseExpression Assignment { get; init; }

    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        // Type check the assignment value
        (var assign, types) = this.Assignment.CheckTypes(types);

        // Make sure assign can unify with our type expression
        if (this.VariableType.TryGetValue(out var typeSyntax)) {
            if (!typeSyntax.AsType(types).TryGetValue(out var type)) {
                throw TypeException.ExpectedTypeExpression(typeSyntax.Location);
            }

            assign = assign.UnifyTo(type, types);
        }

        // Make sure we're not shadowing anybody
        if (types.TryResolveName(types.Scope, this.VariableName, out _)) {
            throw TypeException.IdentifierDefined(this.Location, this.VariableName);
        }

        var path = types.Scope.Append(this.VariableName);
        var sig = new PointerType(assign.ReturnType.GetSignature(types));

        types = types.WithDeclaration(path, new NominalType(path, NominalTypeKind.Variable));
        types = types.WithSignature(path, sig);
        types = types.WithRefinement(path, new PointerType(assign.ReturnType));

        var result = new TypedTree.Variables.TypedVariableStatement {
            Location = this.Location,
            Path = path,
            Assignment = assign,
            VariableSignature = sig
        };

        return new(result, types);
    }
}