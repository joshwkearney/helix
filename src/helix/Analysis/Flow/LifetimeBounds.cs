namespace Helix.Analysis.Flow {
    public class LifetimeBounds {
        public Lifetime RValue { get; set; } = Lifetime.None;

        public Lifetime LValue { get; set; } = Lifetime.None;

        public LifetimeBounds() { }

        public LifetimeBounds(Lifetime lval, Lifetime rval) {
            this.RValue = rval;
            this.LValue = lval;
        }
    }
}
