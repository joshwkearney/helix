using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Helix.MiddleEnd.FlowTyping {
    public record BooleanVariablePredicate : ICnfLeaf {
        public IValueLocation Variable { get; }

        public bool IsNegated { get; }

        public BooleanVariablePredicate(IValueLocation var, bool isNegated = false) {
            this.Variable = var;
            this.IsNegated = isNegated;
        }

        public override ICnfLeaf Negate() => new BooleanVariablePredicate(this.Variable, !this.IsNegated);

        public override bool TryAndWith(ICnfLeaf other, [NotNullWhen(true)] out ICnfLeaf? result) {
            if (other is BooleanVariablePredicate pred) {
                if (pred.Variable == this.Variable && pred.IsNegated != this.IsNegated) {
                    result = new BooleanLiteralPredicate(false);
                    return true;
                }
            }

            result = default;
            return false;
        }

        public override bool TryOrWith(ICnfLeaf other, [NotNullWhen(true)] out ICnfLeaf? result) {
            if (other is BooleanVariablePredicate pred) {
                if (pred.Variable == this.Variable && pred.IsNegated != this.IsNegated) {
                    result = new BooleanLiteralPredicate(true);
                    return true;
                }
            }

            result = default;
            return false;
        }

        public sealed override string ToString() {
            return (this.IsNegated ? "!" : "") + this.Variable;
        }

        public override bool UsesVariable(IValueLocation location) => this.Variable == location;

        public override bool TryGetImplication([NotNullWhen(true)] out IValueLocation? location, [NotNullWhen(true)] out IHelixType? type) {
            location = this.Variable;
            type = new SingularBoolType(!this.IsNegated);

            return !this.IsNegated;
        }
    }
}
