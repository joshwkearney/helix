namespace Helix.Analysis.Predicates
{
    public class EmptyPredicate : ISyntaxPredicate {
        public override ISyntaxPredicate And(ISyntaxPredicate other) => other;

        public override bool Equals(ISyntaxPredicate other) {
            return other is EmptyPredicate;
        }

        public override bool Equals(object other) {
            return other is EmptyPredicate;
        }

        public override int GetHashCode() => 0;

        public override ISyntaxPredicate Negate() => this;

        public override ISyntaxPredicate Or(ISyntaxPredicate other) => other;
    }
}
