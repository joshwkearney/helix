using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.FlowTyping {
    public record EmptyPredicate : ICnfLeaf {
        public override ICnfLeaf Negate() => this;

        public override bool TryAndWith(ICnfLeaf other, [NotNullWhen(true)] out ICnfLeaf? result) {
            result = other;
            return true;
        }

        public override bool TryGetImplication([NotNullWhen(true)] out IValueLocation? location, [NotNullWhen(true)] out IHelixType? type) {
            location = default;
            type = default;
            return false;
        }

        public override bool TryOrWith(ICnfLeaf other, [NotNullWhen(true)] out ICnfLeaf? result) {
            result = other;
            return true;
        }

        public override bool UsesVariable(IValueLocation location) => false;
    }
}
