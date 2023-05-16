using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Helix.Analysis.Flow {
    public class Bundle<T> : IReadOnlyDictionary<IdentifierPath, T> {
        private readonly IReadOnlyDictionary<IdentifierPath, T> items;

        public IEnumerable<IdentifierPath> Keys => this.items.Keys;

        public IEnumerable<T> Values => this.items.Values;

        public int Count => this.items.Count;

        public T this[IdentifierPath key] => this.items[key];

        public Bundle(IReadOnlyDictionary<IdentifierPath, T> lifetimes) {
            this.items = lifetimes;
        }

        public Bundle(T lifetime) {
            this.items = new Dictionary<IdentifierPath, T>() {
                { new IdentifierPath(), lifetime }
            };
        }

        public bool ContainsKey(IdentifierPath key) => this.items.ContainsKey(key);

        public bool TryGetValue(IdentifierPath key, [MaybeNullWhen(false)] out T value) {
            return this.items.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<IdentifierPath, T>> GetEnumerator() {
            return this.items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();
    }

    public class LifetimeBundle : Bundle<Lifetime> {
        public LifetimeBundle(IReadOnlyDictionary<IdentifierPath, Lifetime> lifetimes) 
            : base(lifetimes) { }

        public LifetimeBundle() : this(new Dictionary<IdentifierPath, Lifetime>()) { }
    }
}