using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.TypeChecking;

namespace Helix.Syntax.ParseTree.FlowControl;

public class ExpressionStatement : IParseStatement {
    public required IParseExpression Expression { get; init; }

    public TokenLocation Location => this.Expression.Location;
    
    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        (var expr, types) = this.Expression.CheckTypes(types);

        var result = new TypedTree.FlowControl.TypedExpressionStatement {
            Expression = expr
        };

        return new(result, types);
    }
}