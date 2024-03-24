using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using System.Diagnostics.CodeAnalysis;

namespace Helix.MiddleEnd.FlowTyping {
    public record BooleanLiteralPredicate : ICnfLeaf {
        public bool IsTrue { get; }

        public BooleanLiteralPredicate(bool isTrue) {
            this.IsTrue = isTrue;
        }

        public override ICnfLeaf Negate() => new BooleanLiteralPredicate(!this.IsTrue);

        public override bool TryAndWith(ICnfLeaf other, [NotNullWhen(true)] out ICnfLeaf? result) {
            if (this.IsTrue) {
                result = other;
            }
            else {
                result = this;
            }

            return true;
        }

        public override bool TryOrWith(ICnfLeaf other, [NotNullWhen(true)] out ICnfLeaf? result) {
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

        public override bool UsesVariable(IValueLocation location) => false;

        public override bool TryGetImplication([NotNullWhen(true)] out IValueLocation? location, [NotNullWhen(true)] out IHelixType? type) {
            location = default;
            type = default;

            return false;
        }
    }
}
