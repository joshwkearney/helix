using Helix.Analysis.Types;

namespace Helix.Analysis.Flow {
    public record LocalInfo {
        public HelixType Type { get; }

        public LocalInfo(HelixType types) {
            this.Type = types;
        }

        public LocalInfo WithType(HelixType type) {
            return new LocalInfo(type);
        }
    }
}
