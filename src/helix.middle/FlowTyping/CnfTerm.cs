using Helix.Common;
using Helix.Common.Collections;
using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;

namespace Helix.MiddleEnd.FlowTyping {
    public readonly record struct CnfTerm {
        // Writing CNF code correctly is cursed
        // PLEASE DO NOT TOUCH

        public static CnfTerm Empty { get; } = new CnfTerm(new CnfPolynomial(new EmptyPredicate()));

        public ValueSet<CnfPolynomial> Operands { get; } = [];

        public bool IsFalse {
            get => this.Operands.Any(x => x.Operands.Count == 1 && x.Operands.First() is BooleanLiteralPredicate pred && !pred.IsTrue);
        }

        public bool IsTrue {
            get => this.Operands.All(x => x.Operands.Count == 1 && x.Operands.First() is BooleanLiteralPredicate pred && pred.IsTrue);
        }

        public CnfTerm() { }

        public CnfTerm(CnfPolynomial operand) {
            this.Operands = new[] { operand }.ToValueSet();
        }

        public CnfTerm(IEnumerable<CnfPolynomial> terms) {
            this.Operands = terms.ToValueSet();
        }

        public bool UsesVariable(IValueLocation location) => this.Operands.Any(x => x.UsesVariable(location));

        public IReadOnlyDictionary<IValueLocation, IHelixType> GetImplications() {
            var results = new Dictionary<IValueLocation, IHelixType>();

            foreach (var poly in this.Operands) {
                if (poly.Operands.Count != 1) {
                    continue;
                }

                var op = poly.Operands.First();

                if (op.TryGetImplication(out var loc, out var type)) {
                    if (results.ContainsKey(loc)) {
                        Assert.IsTrue(results[loc] == type);
                    }

                    results[loc] = type;
                }
            }

            return results;
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

            var result = (CnfTerm)new BooleanLiteralPredicate(true);

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

        public CnfTerm Or(ICnfLeaf leaf) {
            return this.Or(new CnfPolynomial(leaf));
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
            if (leaf is BooleanLiteralPredicate lit) {
                return lit.IsTrue ? this : (CnfTerm)lit;
            }

            foreach (var op in this.Operands) {
                if (op.Operands.Count != 1) {
                    continue;
                }

                if (leaf.TryAndWith(op.Operands.First(), out var result)) {
                    return new CnfTerm(this.Operands.Remove(op).Add(new CnfPolynomial(result)));
                }
                else if (op.Operands.First().TryAndWith(leaf, out result)) {
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
