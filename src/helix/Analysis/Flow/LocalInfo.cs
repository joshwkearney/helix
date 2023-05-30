using Helix.Analysis.Types;

namespace Helix.Analysis.Flow {
    public record LocalInfo {
        public LifetimeBounds Bounds { get; }

        public HelixType Type { get; }

        public LocalInfo(HelixType types, LifetimeBounds bounds) {
            this.Bounds = bounds;
            this.Type = types;
        }

        public LocalInfo(HelixType types) {
            this.Bounds = new LifetimeBounds();
            this.Type = types;
        }

        public LocalInfo WithBounds(LifetimeBounds bounds) {
            return new LocalInfo(this.Type, bounds);
        }

        public LocalInfo WithType(HelixType type) {
            return new LocalInfo(type, this.Bounds);
        }
    }
}
