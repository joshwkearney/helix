using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Parsing;
using Helix.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis.Predicates {
    public abstract class SyntaxPredicateLeaf : ISyntaxPredicate {
        public override bool Test(ISyntaxPredicate other) {
            return this.Equals(other);
        }

        public abstract bool TryOrWith(ISyntaxPredicate other, out ISyntaxPredicate result);

        public abstract bool TryAndWith(ISyntaxPredicate other, out ISyntaxPredicate result);
    }

    public abstract class ISyntaxPredicate : IEquatable<ISyntaxPredicate> {
        public static ISyntaxPredicate Empty { get; } = new EmptyPredicate();

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

        public abstract bool Test(ISyntaxPredicate other);

        public abstract ISyntaxPredicate Negate();

        public abstract override bool Equals(object other);

        public abstract override int GetHashCode();

        public abstract bool Equals(ISyntaxPredicate other);

        public static bool operator==(ISyntaxPredicate first, ISyntaxPredicate second) {
            return first.Equals(second);
        }

        public static bool operator !=(ISyntaxPredicate first, ISyntaxPredicate second) {
            return !first.Equals(second);
        }
    }
}
