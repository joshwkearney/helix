using System.Collections;
using System.Collections.Immutable;

namespace Helix.Analysis {
    public class ValueList<T> : IEquatable<ValueList<T>>, IEnumerable<T>, IReadOnlyList<T>,
                                IReadOnlyCollection<T>, IImmutableList<T> {

        private readonly int hashCode;
        private readonly IImmutableList<T> items;

        public ValueList() : this(ImmutableArray<T>.Empty) { }

        public ValueList(IEnumerable<T> values) : this(values.ToImmutableArray()) { }

        public ValueList(IImmutableList<T> values)
            : this(values, values.Aggregate(982451653, (x, y) => x + 13 * y.GetHashCode())) { }

        private ValueList(IImmutableList<T> values, int hash) {
            this.items = values;
            this.hashCode = hash;
        }

        public int Count => this.items.Count;

        public T this[int index] => this.items[index];

        public override int GetHashCode() => this.hashCode;

        public override bool Equals(object obj) {
            if (obj is ValueList<T> other) {
                return this.Equals(other);
            }
            else if (obj is IEnumerable<T> otherSeq) {
                return this.SequenceEqual(otherSeq);
            }

            return false;
        }

        public bool Equals(ValueList<T> other) {
            if (this.hashCode != other.hashCode) {
                return false;
            }
            else {
                return this.items.SequenceEqual(other.items);
            }
        }

        public IEnumerator<T> GetEnumerator() => this.items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

        public ValueList<T> Add(T value) => new ValueList<T>(this.items.Add(value));

        public ValueList<T> AddRange(IEnumerable<T> items) {
            return new ValueList<T>(this.items.AddRange(items));
        }

        public ValueList<T> Clear() => new ValueList<T>();

        public int IndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) {
            return this.items.IndexOf(item, index, count, equalityComparer);
        }

        public ValueList<T> Insert(int index, T element) {
            return new ValueList<T>(this.items.Insert(index, element));
        }

        public ValueList<T> InsertRange(int index, IEnumerable<T> items) {
            return new ValueList<T>(this.items.InsertRange(index, items));
        }

        public int LastIndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) {
            return this.items.LastIndexOf(item, index, count, equalityComparer);
        }

        public ValueList<T> Remove(T value, IEqualityComparer<T> equalityComparer) {
            return new ValueList<T>(this.items.Remove(value, equalityComparer));
        }

        public ValueList<T> RemoveAll(Predicate<T> match) {
            return new ValueList<T>(this.items.RemoveAll(match));
        }

        public ValueList<T> RemoveAt(int index) {
            return new ValueList<T>(this.items.RemoveAt(index));
        }

        public ValueList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer) {
            return new ValueList<T>(this.items.RemoveRange(items, equalityComparer));
        }

        public ValueList<T> RemoveRange(int index, int count) {
            return new ValueList<T>(this.RemoveRange(index, count));
        }

        public ValueList<T> Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer) {
            return new ValueList<T>(this.items.Replace(oldValue, newValue, equalityComparer));
        }

        public ValueList<T> SetItem(int index, T value) {
            return new ValueList<T>(this.items.SetItem(index, value));
        }

        IImmutableList<T> IImmutableList<T>.Add(T value) => this.Add(value);

        IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items) => this.AddRange(items);

        IImmutableList<T> IImmutableList<T>.Clear() => this.Clear();

        IImmutableList<T> IImmutableList<T>.Insert(int index, T element) {
            return this.Insert(index, element);
        }

        IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items) {
            return this.InsertRange(index, items);
        }

        IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T> equalityComparer) {
            return this.Remove(value, equalityComparer);
        }

        IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match) {
            return this.RemoveAll(match);
        }

        IImmutableList<T> IImmutableList<T>.RemoveAt(int index) {
            return this.RemoveAt(index);
        }

        IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer) {
            return this.RemoveRange(items, equalityComparer);
        }

        IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count) {
            return this.RemoveRange(index, count);
        }

        IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer) {
            return this.Replace(oldValue, newValue, equalityComparer);
        }

        IImmutableList<T> IImmutableList<T>.SetItem(int index, T value) {
            return this.SetItem(index, value);
        }

        public static bool operator ==(ValueList<T> list1, ValueList<T> list2) {
            return list1.Equals(list2);
        }

        public static bool operator !=(ValueList<T> list1, ValueList<T> list2) {
            return !list1.Equals(list2);
        }
    }
}