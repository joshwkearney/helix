using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Collections;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Analysis.Predicates {
    public class PredicateTerm : ISyntaxPredicate {
        public ValueSet<PredicatePolynomial> Operands { get; }

        public PredicateTerm() {
            this.Operands = new ValueSet<PredicatePolynomial>();
        }

        public PredicateTerm(PredicatePolynomial operand) {
            this.Operands = new[] { operand }.ToValueSet();
        }

        public PredicateTerm(IEnumerable<PredicatePolynomial> terms) {
            this.Operands = terms.ToValueSet();
        }

        public override ISyntaxPredicate Negate() {
            return this.Operands
                .Select(x => x.Negate())
                .Aggregate((x, y) => x.Or(y));
        }

        public override ISyntaxPredicate Or(ISyntaxPredicate other) {
            if (other == Empty) {
                return this;
            }
            else if (other is PredicatePolynomial poly) {
                return poly.Or(this);
            }
            else if (other is PredicateTerm term) {
                return this.Operands
                    .Select(other.Or)
                    .Aggregate((x, y) => x.And(y));
            }
            else {
                return new PredicatePolynomial(other).Or(this);
            }
        }

        public override ISyntaxPredicate And(ISyntaxPredicate other) {
            if (other == Empty) {
                return this;
            }
            else if (other is PredicatePolynomial poly) {
                if (poly.Operands.Count == 1) {
                    return this.And(poly.Operands.First());
                }
                else {
                    return new PredicateTerm(this.Operands.Add(poly));
                }
            }
            else if (other is PredicateTerm term) {
                return term.Operands.Aggregate((ISyntaxPredicate)this, (x, y) => x.And(y));
            }
            else if (other is SyntaxPredicateLeaf leaf) {
                foreach (var op in this.Operands) {
                    if (op.Operands.Count != 1) {
                        continue;
                    }

                    if (leaf.TryAndWith(op.Operands.First(), out var result)) {
                        var newOps = this.Operands
                            .Remove(op)
                            .Add(new PredicatePolynomial(result));

                        return new PredicateTerm(newOps);
                    }
                }
            }

            return new PredicateTerm(this.Operands.Add(new PredicatePolynomial(other)));
        }

        public override IReadOnlyList<ISyntaxTree> ApplyToTypes(TokenLocation loc, TypeFrame types) {
            return this.Operands
                .SelectMany(x => x.ApplyToTypes(loc, types))
                .ToArray();
        }

        public override bool Equals(ISyntaxPredicate other) {
            if (other is PredicateTerm term) {
                return this.Operands == term.Operands;
            }

            return false;
        }

        public override bool Equals(object other) {
            if (other is PredicateTerm term) {
                return this.Operands == term.Operands;
            }

            return false;
        }

        public override int GetHashCode() => this.Operands.GetHashCode();

        public override string ToString() {
            return string.Join(" and ", this.Operands);
        }

        public override bool Test(ISyntaxPredicate other) {
            return this.Operands.Any(x => x.Test(other));
        }
    }
}
