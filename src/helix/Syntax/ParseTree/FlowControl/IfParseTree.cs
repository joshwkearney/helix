using Helix.Parsing;
using Helix.Syntax.TypedTree.FlowControl;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;
using Helix.TypeChecking.Predicates;
using Helix.Types;

namespace Helix.Syntax.ParseTree.FlowControl;

public record IfParseTree : IParseTree {
    public required TokenLocation Location { get; init; }
        
    public required IParseTree Condition { get; init; }
        
    public required IParseTree Affirmative { get; init; }

    public Option<IParseTree> Negative { get; init; } = Option.None;

    public bool IsPure => this.Affirmative.IsPure && this.Condition.IsPure && this.Negative.Select(x => x.IsPure).OrElse(() => true);

    public TypeCheckResult CheckTypes(TypeFrame types) {
        (var cond, types) = this.Condition.CheckTypes(types);
        var condPredicate = ISyntaxPredicate.Empty;

        var neg = this.Negative.OrElse(() => new VoidLiteral {
            Location = this.Location
        });
        
        // If the condition is a constant, only type check one of our branches 
        if (cond.ReturnType is SingularBoolType sing) {
            ITypedTree next;
            
            if (sing.Value) {
                (next, types) = this.Affirmative.CheckTypes(types);
            }
            else {
                (next, types) = neg.CheckTypes(types);
            }
            
            // We have to put the condition in a block in case it has side effects.
            // If it doesn't, no big deal it will get removed later in dead code
            // elimination
            var block = new BlockTypedTree {
                Location = cond.Location.Span(next.Location),
                AlwaysJumps = cond.AlwaysJumps || next.AlwaysJumps,
                First = cond,
                Second = next
            };

            return new TypeCheckResult(block, types);
        }
        
        // Make sure to apply any predicates from the condition
        if (cond.ReturnType is PredicateBool predBool) {
            condPredicate = predBool.Predicate;
        }

        cond = cond.UnifyTo(PrimitiveType.Bool, types);

        var ifTrueTypes = types.WithScope("$if_aff");
        var ifFalseTypes = types.WithScope("$if_neg");

        ifTrueTypes = condPredicate.ApplyToTypes(ifTrueTypes);
        ifFalseTypes = condPredicate.Negate().ApplyToTypes(ifFalseTypes);
        
        (var checkedIfTrue, ifTrueTypes) = this.Affirmative.CheckTypes(ifTrueTypes);
        (var checkedIfFalse, ifFalseTypes) = neg.CheckTypes(ifFalseTypes);

        ifTrueTypes = ifTrueTypes.PopScope();
        ifFalseTypes = ifFalseTypes.PopScope();
        
        checkedIfTrue = checkedIfTrue.UnifyFrom(checkedIfFalse, types);
        checkedIfFalse = checkedIfFalse.UnifyFrom(checkedIfTrue, types);

        if (!checkedIfTrue.AlwaysJumps && !checkedIfFalse.AlwaysJumps) {
            // If neither branch jumps, we have to combine the signatures
            types = ifTrueTypes.CombineValuesWith(ifFalseTypes);
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
        
        var result = new IfTypedTree {
            Location = this.Location,
            Condition = cond,
            Affirmative = checkedIfTrue,
            Negative = checkedIfFalse,
            ReturnType = checkedIfTrue.ReturnType,
            AlwaysJumps = checkedIfTrue.AlwaysJumps && checkedIfFalse.AlwaysJumps
        };
        
        return new TypeCheckResult(result, types);
    }
}