using Helix.CodeGeneration;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.TypedTree.FlowControl;
using Helix.TypeChecking;

namespace Helix.Syntax.ParseTree.FlowControl;

public class ExpressionParseStatement : IParseStatement {
    public required IParseTree Expression { get; init; }

    public TokenLocation Location => this.Expression.Location;
    
    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        (var expr, types) = this.Expression.CheckTypes(types);

        var result = new ExpressionStatement {
            Expression = expr
        };

        return new(result, types);
    }
}