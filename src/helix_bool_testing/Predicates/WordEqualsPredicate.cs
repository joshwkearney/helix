using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.Predicates {
    internal record BooleanLiteral : ICnfLeaf {
        public bool IsTrue { get; }

        public BooleanLiteral(bool isTrue) {
            this.IsTrue = isTrue;
        }

        public override ICnfLeaf Negate() => new BooleanLiteral(!this.IsTrue);

        public override bool TryAndWith(ICnfLeaf other, out ICnfLeaf result) {
            if (this.IsTrue) {
                result = other;
            }
            else {
                result = this;
            }

            return true;
        }

        public override bool TryOrWith(ICnfLeaf other, out ICnfLeaf result) {
            if (this.IsTrue) {
                result = this;
            }
            else {
                result = other;
            }

            return true;
        }

        public sealed override string ToString() {
            return this.IsTrue.ToString().ToLower();
        }
    }

    internal record BooleanPredicate : ICnfLeaf {
        public string Variable { get; }

        public bool IsNegated { get; }

        public BooleanPredicate(string var, bool isNegated = false) {
            this.Variable = var;
            this.IsNegated = isNegated;
        }

        public override ICnfLeaf Negate() => new BooleanPredicate(this.Variable, !this.IsNegated);

        public override bool TryAndWith(ICnfLeaf other, out ICnfLeaf result) {
            if (other is BooleanPredicate pred) {
                if (pred.Variable == this.Variable && pred.IsNegated != this.IsNegated) {
                    result = new BooleanLiteral(false);
                    return true;
                }
            }

            result = default;
            return false;
        }

        public override bool TryOrWith(ICnfLeaf other, out ICnfLeaf result) {
            if (other is BooleanPredicate pred) {
                if (pred.Variable == this.Variable && pred.IsNegated != this.IsNegated) {
                    result = new BooleanLiteral(true);
                    return true;
                }
            }

            result = default;
            return false;
        }

        public sealed override string ToString() {
            return (this.IsNegated ? "!" : "") + this.Variable;
        }
    }

    internal record WordEqualsPredicate : ICnfLeaf {
        public string Variable { get; }

        public long Value { get; }

        public bool IsNegated { get; }

        public WordEqualsPredicate(string var, long value, bool isnegated = false) {
            this.Variable = var;
            this.Value = value;
            this.IsNegated = isnegated;
        }

        public override ICnfLeaf Negate() {
            return new WordEqualsPredicate(this.Variable, this.Value, !this.IsNegated);
        }

        public override bool TryOrWith(ICnfLeaf other, out ICnfLeaf result) {
            if (other is WordEqualsPredicate pred) {
                if (pred.Variable == this.Variable && pred.Value == this.Value && pred.IsNegated != this.IsNegated) {
                    result = new BooleanLiteral(true);
                    return true;
                }
            }

            result = default;
            return false;
        }

        public override bool TryAndWith(ICnfLeaf other, out ICnfLeaf result) {
            if (other is WordEqualsPredicate pred) {
                if (pred.Variable == this.Variable && pred.Value == this.Value && pred.IsNegated != this.IsNegated) {
                    result = new BooleanLiteral(false);
                    return true;
                }
            }

            result = default;
            return false;
        }

        public sealed override string ToString() {
            if (this.IsNegated) {
                return $"{this.Variable} == {this.Value}";
            }
            else {
                return $"{this.Variable} != {this.Value}";
            }
        }
    }
}
