namespace Helix.TypeChecking.Predicates {
    public abstract record ISyntaxPredicate : IEquatable<ISyntaxPredicate> {
        public static ISyntaxPredicate Empty { get; } = new EmptyPredicate();

        public abstract ISyntaxPredicate Negate();

        public virtual ISyntaxPredicate And(ISyntaxPredicate other) {
            return new PredicateTerm(new PredicatePolynomial(this)).And(other);
        }

        public virtual ISyntaxPredicate Or(ISyntaxPredicate other) {
            return new PredicatePolynomial(this).Or(other);
        }

        public virtual ISyntaxPredicate Xor(ISyntaxPredicate other) {
            var left = this.And(other.Negate());
            var right = this.Negate().And(other);

            return left.Or(right);
        }

        public virtual TypeFrame ApplyToTypes(TypeFrame types) {
            return types;
        }

        public abstract bool Equals(ISyntaxPredicate other);
    }
}
