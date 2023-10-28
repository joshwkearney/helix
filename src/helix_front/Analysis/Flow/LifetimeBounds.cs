namespace Helix.Analysis.Flow {
    public readonly record struct LifetimeBounds {
        public Lifetime ValueLifetime { get; }

        public Lifetime LocationLifetime { get; }

        public LifetimeBounds(Lifetime valueLifetime, Lifetime locationLifetime) {
            this.ValueLifetime = valueLifetime;
            this.LocationLifetime = locationLifetime;
        }

        public LifetimeBounds(Lifetime valueLifetime) : this(valueLifetime, Lifetime.None) { }

        public LifetimeBounds() : this(Lifetime.None) { }

        public LifetimeBounds AsRValue() {
            return new LifetimeBounds(this.ValueLifetime);
        }

        public LifetimeBounds WithValue(Lifetime valueLifetime) {
            return new LifetimeBounds(valueLifetime, this.LocationLifetime);
        }

        public LifetimeBounds WithLocation(Lifetime locationLifetime) {
            return new LifetimeBounds(this.ValueLifetime, locationLifetime);
        }
    }
}
