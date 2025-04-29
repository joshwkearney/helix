﻿using Helix.Collections;

namespace Helix.TypeChecking.Predicates {
    public record PredicateTerm : ISyntaxPredicate {
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
            if (other is PredicatePolynomial poly) {
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
            if (other is PredicatePolynomial poly) {
                return new PredicateTerm(this.Operands.Add(poly));
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

            return this.And(new PredicatePolynomial(other));
        }

        public override TypeFrame ApplyToTypes(TypeFrame types) {
            if (this.Operands.Count == 0) {
                return types;
            }
            
            return this.Operands
                .Select(x => x.ApplyToTypes(types))
                .Aggregate((x, y) => x.CombineRefinementsWith(y));
        }

        public override string ToString() {
            return string.Join(" and ", this.Operands);
        }
    }
}
