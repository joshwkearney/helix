using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using System.Diagnostics.CodeAnalysis;

namespace Helix.MiddleEnd.FlowTyping {
    public record WordVariablePredicate : ICnfLeaf {
        public IValueLocation Variable { get; }

        public long Value { get; }

        public bool IsNegated { get; }

        public WordVariablePredicate(IValueLocation var, long value, bool isnegated = false) {
            this.Variable = var;
            this.Value = value;
            this.IsNegated = isnegated;
        }

        public override ICnfLeaf Negate() {
            return new WordVariablePredicate(this.Variable, this.Value, !this.IsNegated);
        }

        public override bool TryOrWith(ICnfLeaf other, [NotNullWhen(true)] out ICnfLeaf? result) {
            if (other is WordVariablePredicate pred) {
                if (pred.Variable == this.Variable && pred.Value == this.Value && pred.IsNegated != this.IsNegated) {
                    result = new BooleanLiteralPredicate(true);
                    return true;
                }
            }

            result = default;
            return false;
        }

        public override bool TryAndWith(ICnfLeaf other, [NotNullWhen(true)] out ICnfLeaf? result) {
            if (other is WordVariablePredicate pred) {
                if (pred.Variable == this.Variable && pred.Value == this.Value && pred.IsNegated != this.IsNegated) {
                    result = new BooleanLiteralPredicate(false);
                    return true;
                }
            }

            result = default;
            return false;
        }

        public sealed override string ToString() {
            if (this.IsNegated) {
                return $"{this.Variable} != {this.Value}";
            }
            else {
                return $"{this.Variable} == {this.Value}";
            }
        }

        public override bool UsesVariable(IValueLocation location) => this.Variable == location;

        public override bool TryGetImplication([NotNullWhen(true)] out IValueLocation location, [NotNullWhen(true)] out IHelixType type) {
            location = this.Variable;
            type = new SingularWordType(this.Value);

            return !this.IsNegated;
        }
    }
}
