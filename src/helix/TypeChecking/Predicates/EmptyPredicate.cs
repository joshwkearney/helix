namespace Helix.TypeChecking.Predicates {
    public record EmptyPredicate : ISyntaxPredicate {
        public override ISyntaxPredicate And(ISyntaxPredicate other) => other;

        public override ISyntaxPredicate Negate() => this;

        public override ISyntaxPredicate Or(ISyntaxPredicate other) => other;
    }
}
