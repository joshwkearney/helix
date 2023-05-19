namespace Helix.Analysis.Flow {
    public record LifetimeBounds(Lifetime RValue, Lifetime LValue) {
        public static LifetimeBounds Empty { get; } = new LifetimeBounds();

        public LifetimeBounds() : this(Lifetime.None, Lifetime.None) { }

        public LifetimeBounds WithRValue(Lifetime rval) {
            return new LifetimeBounds(rval, this.LValue);
        }

        public LifetimeBounds WithLValue(Lifetime lval) {
            return new LifetimeBounds(this.RValue, lval);
        }
    }
}
