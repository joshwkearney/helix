using Helix.Common;
using Helix.Common.Collections;
using Helix.MiddleEnd.Interpreting;

namespace Helix.MiddleEnd.FlowTyping {
    public readonly record struct CnfPolynomial {
        public ValueSet<ICnfLeaf> Operands { get; }

        public CnfPolynomial(ICnfLeaf operand) {
            this.Operands = new[] { operand }.ToValueSet();
        }

        public CnfPolynomial(IEnumerable<ICnfLeaf> operands) {
            this.Operands = operands.ToValueSet();
        }

        public bool UsesVariable(IValueLocation location) => this.Operands.Any(x => x.UsesVariable(location));

        public CnfTerm And(CnfTerm term) { 
            return term.And(this);
        }

        public CnfTerm And(CnfPolynomial other) {
            if (other.Operands.Count == 1) {
                return new CnfTerm(this).And(other.Operands.First());
            }
            else if (this.Operands.Count == 1) {
                return new CnfTerm(other).And(this.Operands.First());
            }

            return new CnfTerm(new[] { this, other });
        }

        public CnfPolynomial Or(ICnfLeaf leaf) {
            if (leaf is BooleanLiteralPredicate lit) {
                return lit.IsTrue ? new CnfPolynomial(lit) : this;
            }

            foreach (var op in this.Operands) {
                if (leaf.TryOrWith(op, out var result)) {
                    return new CnfPolynomial(this.Operands.Remove(op).Add(result));
                }
                else if (op.TryOrWith(leaf, out result)) {
                    return new CnfPolynomial(this.Operands.Remove(op).Add(result));
                }
            }

            return new CnfPolynomial(this.Operands.Add(leaf));
        }

        public CnfPolynomial Or(CnfPolynomial poly) {
            return poly.Operands.Aggregate(this, (x, y) => x.Or(y));
        }

        public CnfTerm Or(CnfTerm term) {
            return term
                .Operands
                .Select(this.Or)
                .Select(x => new CnfTerm(x))
                .Aggregate((x, y) => x.And(y));
        }

        public CnfTerm Negate() {
            var ops = this.Operands
                .Select(x => x.Negate())
                .Select(x => new CnfPolynomial(x));

            return new CnfTerm(ops);
        }

        public override string ToString() {
            return "(" + string.Join(" or ", this.Operands) + ")";
        }

        public bool Test(ICnfLeaf other) {
            return this.Operands.All(x => x.Test(other));
        }
    }
}