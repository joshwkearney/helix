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

    public ISyntax CheckTypes(TypeFrame types) {
        var cond = this.Condition.CheckTypes(types).ToRValue(types);
        var condPredicate = ISyntaxPredicate.Empty;

        if (cond.ReturnType is PredicateBool predBool) {
            condPredicate = predBool.Predicate;
        }

        cond = cond.UnifyTo(PrimitiveType.Bool, types);

        var iftrueTypes = new TypeFrame(types, "$if_aff");
        var iffalseTypes = new TypeFrame(types, $"$if_neg");

        var ifTruePrepend = condPredicate.ApplyToTypes(this.Condition.Location, iftrueTypes);
        var ifFalsePrepend = condPredicate.Negate().ApplyToTypes(this.Condition.Location, iffalseTypes);

        var iffalse = this.Negative.OrElse(() => new VoidLiteral { Location = this.Location });

        var iftrue = BlockParseSyntax.FromMany(this.Affirmative.Location, ifTruePrepend.Append(this.Affirmative).ToArray());
        iffalse = BlockParseSyntax.FromMany(iffalse.Location, ifFalsePrepend.Append(iffalse).ToArray());

        var checkedIfTrue = iftrue.CheckTypes(iftrueTypes).ToRValue(iftrueTypes);
        var checkedIfFalse = iffalse.CheckTypes(iffalseTypes).ToRValue(iffalseTypes);

        checkedIfTrue = checkedIfTrue.UnifyFrom(checkedIfFalse, types);
        checkedIfFalse = checkedIfFalse.UnifyFrom(checkedIfTrue, types);

        var result = new IfSyntax {
            Location = this.Location,
            Condition = cond,
            Affirmative = checkedIfTrue,
            Negative = checkedIfFalse,
            ReturnType = checkedIfTrue.ReturnType
        };

        return result;
    }
}