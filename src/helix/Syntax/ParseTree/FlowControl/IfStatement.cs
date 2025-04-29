using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.FlowControl;
using Helix.TypeChecking;
using Helix.TypeChecking.Predicates;
using Helix.Types;

namespace Helix.Syntax.ParseTree.FlowControl;

public record IfStatement : IParseStatement {
    public required TokenLocation Location { get; init; }
        
    public required IParseExpression Condition { get; init; }
        
    public required IParseStatement Affirmative { get; init; }

    public required IParseStatement Negative { get; init; }
    
    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        (var cond, types) = this.Condition.CheckTypes(types);
        var condPredicate = ISyntaxPredicate.Empty;
        
        // If the condition is a constant, only type check one of our branches 
        if (cond.ReturnType is SingularBoolType sing) {
            (var next, types) = sing.Value
                ? this.Affirmative.CheckTypes(types)
                : this.Negative.CheckTypes(types);
            
            // We have to put the condition in a block in case it has side effects.
            // If it doesn't, no big deal it will get removed later in dead code
            // elimination
            var block = new TypedBlockStatement {
                Location = cond.Location.Span(next.Location),
                AlwaysJumps = next.AlwaysJumps,
                Statements = [
                    new TypedTree.FlowControl.TypedExpressionStatement {
                        Expression = cond
                    },
                    next
                ]
            };

            return new TypeCheckResult<ITypedStatement>(block, types);
        }
        
        // Make sure to apply any predicates from the condition
        if (cond.ReturnType is PredicateBool predBool) {
            condPredicate = predBool.Predicate;
        }

        cond = cond.UnifyTo(PrimitiveType.Bool, types);

        var ifTrueTypes = condPredicate.ApplyToTypes(types.WithScope("$if_aff"));
        var ifFalseTypes = condPredicate.Negate().ApplyToTypes(types.WithScope("$if_neg"));
        
        (var checkedIfTrue, ifTrueTypes) = this.Affirmative.CheckTypes(ifTrueTypes);
        (var checkedIfFalse, ifFalseTypes) = this.Negative.CheckTypes(ifFalseTypes);

        ifTrueTypes = ifTrueTypes.PopScope();
        ifFalseTypes = ifFalseTypes.PopScope();

        if (!checkedIfTrue.AlwaysJumps && !checkedIfFalse.AlwaysJumps) {
            // If neither branch jumps, we have to combine the signatures
            types = ifTrueTypes.CombineRefinementsWith(ifFalseTypes);
        }
        else if (!checkedIfTrue.AlwaysJumps && checkedIfFalse.AlwaysJumps) {
            // If the first branch doesn't jump but the second does, take the first types
            types = ifTrueTypes;
        }
        else if (checkedIfTrue.AlwaysJumps && !checkedIfFalse.AlwaysJumps) {
            // If the first branch jumps but not the second does, take the second types
            types = ifFalseTypes;
        }
        else {
            // If both branches jump, leave types alone because none of the context propagates
        }

        // We want to track break and continue context from both branches
        types = types
            .CombineLoopFramesWith(ifTrueTypes)
            .CombineLoopFramesWith(ifFalseTypes);
        
        var result = new TypedIfStatement {
            Location = this.Location,
            Condition = cond,
            Affirmative = checkedIfTrue,
            Negative = checkedIfFalse,
            AlwaysJumps = checkedIfTrue.AlwaysJumps && checkedIfFalse.AlwaysJumps
        };
        
        return new(result, types);
    }
}