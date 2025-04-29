using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.FlowControl;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;
using Helix.TypeChecking.Predicates;
using Helix.Types;

namespace Helix.Syntax.ParseTree.FlowControl;

public record IfExpression : IParseExpression {
    public required TokenLocation Location { get; init; }
        
    public required IParseExpression Condition { get; init; }
        
    public required IParseExpression Affirmative { get; init; }

    public required IParseExpression Negative { get; init; }
    
    public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
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
            var block = new TypedCompoundExpression {
                First = cond,
                Second = next
            };

            return new TypeCheckResult<ITypedExpression>(block, types);
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
        
        checkedIfTrue = checkedIfTrue.UnifyFrom(checkedIfFalse, types);
        checkedIfFalse = checkedIfFalse.UnifyFrom(checkedIfTrue, types);

        // If expression won't early return, so we need to combine the types
        // from each branch
        types = ifTrueTypes.CombineRefinementsWith(ifFalseTypes)
            .CombineLoopFramesWith(ifTrueTypes)
            .CombineLoopFramesWith(ifFalseTypes);
        
        var result = new TypedIfExpression {
            Location = this.Location,
            Condition = cond,
            Affirmative = checkedIfTrue,
            Negative = checkedIfFalse,
            ReturnType = checkedIfTrue.ReturnType
        };
        
        return new(result, types);
    }
}