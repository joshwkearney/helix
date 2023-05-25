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
        public abstract bool TryOrWith(ISyntaxPredicate other, out ISyntaxPredicate result);

        public abstract bool TryAndWith(ISyntaxPredicate other, out ISyntaxPredicate result);
    }

    public abstract class ISyntaxPredicate : IEquatable<ISyntaxPredicate> {
        public static ISyntaxPredicate Empty { get; } = new EmptyPredicate();

        public abstract ISyntaxPredicate Negate();

        public virtual ISyntaxPredicate And(ISyntaxPredicate other) {
            return new PredicateTerm(new PredicatePolynomial(this)).And(other);
        }

        public virtual ISyntaxPredicate Or(ISyntaxPredicate other) {
            return new PredicatePolynomial(this).Or(other);
        }

        public virtual IReadOnlyList<ISyntaxTree> ApplyToTypes(TokenLocation loc, TypeFrame types) {
            return Array.Empty<ISyntaxTree>();
        }

        public virtual void ApplyToFlow(FlowFrame flow) { }

        public abstract override bool Equals(object other);

        public abstract override int GetHashCode();

        public abstract bool Equals(ISyntaxPredicate other);
    }
}
