using Helix.Analysis.TypeChecking;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Analysis.Predicates {
    public abstract class ISyntaxPredicate : IEquatable<ISyntaxPredicate> {
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

        public virtual IReadOnlyList<ISyntaxTree> ApplyToTypes(TokenLocation loc, TypeFrame types) {
            return Array.Empty<ISyntaxTree>();
        }

        public abstract override bool Equals(object other);

        public abstract override int GetHashCode();

        public abstract bool Equals(ISyntaxPredicate other);
    }
}
