using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.FlowControl;

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

        ifTrueTypes = ifFalseTypes.PopScope();
        ifFalseTypes = ifFalseTypes.PopScope();
        types = ifTrueTypes.CombineWith(ifFalseTypes);
        
        checkedIfTrue = checkedIfTrue.UnifyFrom(checkedIfFalse, types);
        checkedIfFalse = checkedIfFalse.UnifyFrom(checkedIfTrue, types);

        var result = new IfSyntax {
            Location = this.Location,
            Condition = cond,
            Affirmative = checkedIfTrue,
            Negative = checkedIfFalse,
            ReturnType = checkedIfTrue.ReturnType
        };
        
        return new TypeCheckResult(result, types);
    }
}