using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Helix.Collections {
    public class StackedDictionary<T, E> : IDictionary<T, E>, IReadOnlyDictionary<T, E> {
        private readonly IDictionary<T, E> values = new Dictionary<T, E>();
        private readonly IDictionary<T, E> prev;

        public StackedDictionary(IDictionary<T, E> prev) {
            this.prev = prev;
        }

        public E this[T key] {
            get {
                if (this.values.TryGetValue(key, out var value)) {
                    return value;
                }
                else {
                    return this.prev[key];
                }
            }
            set {
                this.values[key] = value;
            }
        }

        public ICollection<T> Keys {
            get => this.values.Keys;
        }

        public ICollection<E> Values {
            get => this.values.Values;
        }

        IEnumerable<T> IReadOnlyDictionary<T, E>.Keys => this.Keys;

        IEnumerable<E> IReadOnlyDictionary<T, E>.Values => this.Values;

        public int Count => this.values.Count;

        public bool IsReadOnly => false;

        public void Add(T key, E value) {
            this.values.Add(key, value);
        }

        public void Add(KeyValuePair<T, E> item) => this.Add(item.Key, item.Value);

        public void Clear() {
            this.values.Clear();
        }

        public bool Contains(KeyValuePair<T, E> item) {
            return this.values.Contains(item) || this.prev.Contains(item);
        }

        public bool ContainsKey(T key) {
            return this.values.ContainsKey(key) || this.prev.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<T, E>[] array, int arrayIndex) {
            this.values.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<T, E>> GetEnumerator() {
            return this.values.GetEnumerator();
        }

        public bool Remove(T key) {
            return this.values.Remove(key);
        }

        public bool Remove(KeyValuePair<T, E> item) {
            return this.values.Remove(item);
        }

        public bool TryGetValue(T key, [MaybeNullWhen(false)] out E value) {
            if (this.values.TryGetValue(key, out value)) {
                return true;
            }

            return this.prev.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}