using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using System.Diagnostics.CodeAnalysis;

namespace Helix.MiddleEnd.FlowTyping {
    public abstract record ICnfLeaf {
        public virtual bool Test(ICnfLeaf other) => this.Equals(other);

        public abstract ICnfLeaf Negate();

        public abstract bool TryOrWith(ICnfLeaf other, [NotNullWhen(true)] out ICnfLeaf? result);

        public abstract bool TryAndWith(ICnfLeaf other, [NotNullWhen(true)] out ICnfLeaf? result);

        public abstract bool UsesVariable(IValueLocation location);

        public abstract bool TryGetImplication([NotNullWhen(true)] out IValueLocation? location, [NotNullWhen(true)] out IHelixType? type);
    }
}
