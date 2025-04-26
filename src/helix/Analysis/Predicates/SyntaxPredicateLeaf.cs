namespace Helix.Analysis.Predicates;

public abstract record SyntaxPredicateLeaf : ISyntaxPredicate {
    public abstract bool TryOrWith(ISyntaxPredicate other, out ISyntaxPredicate result);

    public abstract bool TryAndWith(ISyntaxPredicate other, out ISyntaxPredicate result);
}