using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.FlowControl;
using Helix.TypeChecking;

namespace Helix.Syntax.ParseTree.FlowControl;

public record BlockStatement : IParseStatement {
    public required TokenLocation Location { get; init; }
        
    public required IReadOnlyList<IParseStatement> Statements { get; init; }
        
    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        var stats = new List<ITypedStatement>();
            
        foreach (var stat in this.Statements) {
            (var checkedStat, types) = stat.CheckTypes(types);
            stats.Add(checkedStat);
                
            // We need to make a new scope for the next statement
            types = types.WithScope("$block");

            // Skip the second statement if the first one returns
            if (checkedStat.AlwaysJumps) {
                break;
            }
        }

        // Pop all our new scopes
        for (int i = 0; i < stats.Count; i++) {
            types = types.PopScope();
        }
            
        var result = new TypedBlockStatement {
            Location = this.Location,
            Statements = stats,
            AlwaysJumps = stats.Any(x => x.AlwaysJumps)
        };

        return new(result, types);
    }
}