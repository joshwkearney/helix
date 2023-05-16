namespace Helix.Analysis.Flow {
    public class LifetimeBundle {
        public IReadOnlyDictionary<IdentifierPath, Lifetime> Components { get; }

        public IEnumerable<Lifetime> Lifetimes => this.Components.Values;

        public LifetimeBundle(IReadOnlyDictionary<IdentifierPath, Lifetime> lifetimes) {
            this.Components = lifetimes;
        }

        public LifetimeBundle() {
            this.Components = new Dictionary<IdentifierPath, Lifetime>() {
                { new IdentifierPath(), Lifetime.None }
            };
        }

        public LifetimeBundle(Lifetime lifetime) {
            this.Components = new Dictionary<IdentifierPath, Lifetime>() {
                { new IdentifierPath(), lifetime }
            };
        }
    }
}