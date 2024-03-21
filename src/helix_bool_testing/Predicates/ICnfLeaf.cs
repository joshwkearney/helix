namespace Helix.MiddleEnd.Predicates {
    public abstract record ICnfLeaf {
        public virtual bool Test(ICnfLeaf other) => this.Equals(other);

        public abstract ICnfLeaf Negate();

        public abstract bool TryOrWith(ICnfLeaf other, out ICnfLeaf result);

        public abstract bool TryAndWith(ICnfLeaf other, out ICnfLeaf result);
    }
}
