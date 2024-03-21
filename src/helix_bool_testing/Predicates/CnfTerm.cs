using Helix.Common;
using Helix.Common.Collections;

namespace Helix.MiddleEnd.Predicates {
    public readonly record struct CnfTerm {
        // Writing CNF code correctly is cursed
        // PLEASE DO NOT TOUCH

        public ValueSet<CnfPolynomial> Operands { get; } = [];

        public CnfTerm() { }

        public CnfTerm(CnfPolynomial operand) {
            this.Operands = new[] { operand }.ToValueSet();
        }

        public CnfTerm(IEnumerable<CnfPolynomial> terms) {
            this.Operands = terms.ToValueSet();
        }

        public CnfTerm Negate() {
            return this.Operands
                .Select(x => x.Negate())
                .Aggregate((x, y) => x.Or(y));
        }

        public CnfTerm Or(CnfTerm term) {
            if (term.Operands.Count == 1) {
                return this.Or(term.Operands.First());
            }
            else if (this.Operands.Count == 1) {
                return term.Or(this.Operands.First());
            }

            CnfTerm result = (CnfTerm)new BooleanLiteral(true);

            foreach (var poly1 in this.Operands) {
                foreach (var poly2 in term.Operands) {
                    var op = poly1.Or(poly2);

                    result = result.And(op);
                }
            }

            return result;
        }

        public CnfTerm Or(CnfPolynomial poly) {
            //if (poly.Operands.Count == 1) {
            //    return this.Or(poly.Operands.First());
            //}
            //else
            if (this.Operands.Count == 1) {
                return poly.Or(this.Operands.First());
            }

            return poly.Or(this);
        }

        public CnfTerm And(CnfTerm term) {
            if (term.Operands.Count == 1) {
                return this.And(term.Operands.First());
            }
            else if (this.Operands.Count == 1) {
                return term.And(this.Operands.First());
            }

            return term.Operands.Aggregate(this, (x, y) => x.And(y));
        }

        public CnfTerm And(CnfPolynomial poly) {
            if (poly.Operands.Count == 1) {
                return this.And(poly.Operands.First());
            }
            else if (this.Operands.Count == 1) {
                return poly.And(this.Operands.First());
            }

            return new CnfTerm(this.Operands.Add(poly));
        }

        public CnfTerm And(ICnfLeaf leaf) {
            if (leaf is BooleanLiteral lit) {
                return lit.IsTrue ? this : (CnfTerm)lit;
            }

            foreach (var op in this.Operands) {
                if (op.Operands.Count != 1) {
                    continue;
                }

                if (leaf.TryAndWith(op.Operands.First(), out var result)) {
                    return new CnfTerm(this.Operands.Remove(op).Add(new CnfPolynomial(result)));
                }
            }

            return new CnfTerm(this.Operands.Add(new CnfPolynomial(leaf)));
        }

        public override string ToString() {
            return string.Join(" and ", this.Operands);
        }

        public bool Test(ICnfLeaf other) {
            return this.Operands.Any(x => x.Test(other));
        }

        public static explicit operator CnfTerm(ICnfLeaf leaf) {
            return new CnfTerm(new CnfPolynomial(leaf));
        }

        public static implicit operator CnfTerm(CnfPolynomial poly) => new(poly);

        public static CnfTerm operator &(CnfTerm first, CnfTerm second) => first.And(second);

        public static CnfTerm operator &(CnfTerm first, CnfPolynomial second) => first.And(second);

        public static CnfTerm operator |(CnfTerm first, CnfTerm second) => first.Or(second);

        public static CnfTerm operator |(CnfTerm first, CnfPolynomial second) => first.Or(second);

        public static CnfTerm operator !(CnfTerm first) => first.Negate();
    }
}
