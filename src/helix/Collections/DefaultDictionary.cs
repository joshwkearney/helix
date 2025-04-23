using System.Collections;

namespace Helix.Collections {
    public class DefaultDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> {
        private readonly IDictionary<TKey, TValue> inner;
        private readonly Func<TKey, TValue> valueFactory;

        public DefaultDictionary(IDictionary<TKey, TValue> inner, Func<TKey, TValue> valueFactory) {
            this.inner = inner;
            this.valueFactory = valueFactory;
        }

        public DefaultDictionary(Func<TKey, TValue> valueFactory) 
            : this(new Dictionary<TKey, TValue>(), valueFactory) { }

        public TValue this[TKey key] { 
            get {
                if (!this.ContainsKey(key)) {
                    this.inner[key] = this.valueFactory(key);
                }

                return this.inner[key];
            }
            set {
                this.inner[key] = value;
            }
        }

        public ICollection<TKey> Keys => this.inner.Keys;

        public ICollection<TValue> Values => this.inner.Values;

        public int Count => this.inner.Count;

        public bool IsReadOnly => this.inner.IsReadOnly;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.Values;

        public void Add(TKey key, TValue value) => this.inner.Add(key, value);

        public void Add(KeyValuePair<TKey, TValue> item) => this.inner.Add(item);

        public void Clear() => this.inner.Clear();

        public bool Contains(KeyValuePair<TKey, TValue> item) => this.inner.Contains(item);

        public bool ContainsKey(TKey key) => this.inner.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            this.inner.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => this.inner.GetEnumerator();

        public bool Remove(TKey key) => this.inner.Remove(key);

        public bool Remove(KeyValuePair<TKey, TValue> item) => this.inner.Remove(item);

        public bool TryGetValue(TKey key, out TValue value) {
            if (this.inner.TryGetValue(key, out value)) {
                return true;
            }
            else {
                this.inner[key] = value = this.valueFactory(key);
                return true;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.inner.GetEnumerator();
    }
}
