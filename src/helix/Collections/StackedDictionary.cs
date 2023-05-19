using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Helix.Collections
{
    public class StackedDictionary<T, E> : IDictionary<T, E> where T : notnull
    {
        private readonly IDictionary<T, E> values = new Dictionary<T, E>();
        private readonly IDictionary<T, E> prev;

        public StackedDictionary(IDictionary<T, E> prev)
        {
            this.prev = prev;
        }

        public E this[T key]
        {
            get
            {
                if (values.TryGetValue(key, out var value))
                {
                    return value;
                }
                else
                {
                    return prev[key];
                }
            }
            set
            {
                values[key] = value;
            }
        }

        public ICollection<T> Keys
        {
            get => values.Keys;
        }

        public ICollection<E> Values
        {
            get => values.Values;
        }

        public int Count => values.Count;

        public bool IsReadOnly => false;

        public void Add(T key, E value)
        {
            values.Add(key, value);
        }

        public void Add(KeyValuePair<T, E> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            values.Clear();
        }

        public bool Contains(KeyValuePair<T, E> item)
        {
            return values.Contains(item) || prev.Contains(item);
        }

        public bool ContainsKey(T key)
        {
            return values.ContainsKey(key) || prev.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<T, E>[] array, int arrayIndex)
        {
            values.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<T, E>> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        public bool Remove(T key)
        {
            return values.Remove(key);
        }

        public bool Remove(KeyValuePair<T, E> item)
        {
            return values.Remove(item);
        }

        public bool TryGetValue(T key, [MaybeNullWhen(false)] out E value)
        {
            if (values.TryGetValue(key, out value))
            {
                return true;
            }

            return prev.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}