namespace Helix.Analysis.Predicates;

public abstract class SyntaxPredicateLeaf : ISyntaxPredicate {
    public abstract bool TryOrWith(ISyntaxPredicate other, out ISyntaxPredicate result);

    public abstract bool TryAndWith(ISyntaxPredicate other, out ISyntaxPredicate result);
}