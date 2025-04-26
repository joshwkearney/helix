using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.FlowControl.Syntax;
using Helix.Features.Primitives.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.FlowControl.ParseSyntax;

public record IfParseSyntax : IParseSyntax {
    public required TokenLocation Location { get; init; }
        
    public required IParseSyntax Condition { get; init; }
        
    public required IParseSyntax Affirmative { get; init; }

    public Option<IParseSyntax> Negative { get; init; } = Option.None;

    public bool IsPure => this.Affirmative.IsPure && this.Condition.IsPure && this.Negative.Select(x => x.IsPure).OrElse(() => true);

    public TypeCheckResult CheckTypes(TypeFrame types) {
        (var cond, types) = this.Condition.CheckTypes(types);
        var condPredicate = ISyntaxPredicate.Empty;

        // TODO: This isn't used
        if (cond.ReturnType is PredicateBool predBool) {
            condPredicate = predBool.Predicate;
        }

        cond = cond.UnifyTo(PrimitiveType.Bool, types);

        var ifTrueTypes = types.WithScope("$if_aff");
        var ifFalseTypes = types.WithScope("$if_neg");
        
        (var checkedIfTrue, ifTrueTypes) = this.Affirmative.CheckTypes(ifTrueTypes);
        
        (var checkedIfFalse, ifFalseTypes) = this.Negative
            .OrElse(() => new VoidLiteral { Location = this.Location })
            .CheckTypes(ifFalseTypes);

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
        
        var result = new IfSyntax {
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