using Helix.Analysis.Types;

namespace Helix.HelixMinusMinus {
    public record HmmVariable {
        public string Name { get; init; }

        public HelixType Type { get; init; }
    }
}
