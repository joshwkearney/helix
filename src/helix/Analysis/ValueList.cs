using System.Collections;
using System.Collections.Immutable;

namespace Helix.Analysis {
    public class ValueList<T> : IEquatable<ValueList<T>>, IEnumerable<T>, IReadOnlyList<T> {
        private readonly int hashCode;
        private readonly ImmutableList<T> items;

        public ValueList() {
            this.items = ImmutableList<T>.Empty;
            this.hashCode = 0;
        }

        public ValueList(IEnumerable<T> values) : this(values.ToImmutableList()) { }

        public ValueList(ImmutableList<T> values) {
            this.items = values;
            this.hashCode = values.Aggregate(13, (x, y) => x + 7 * y.GetHashCode());
        }

        public T this[int index] => this.items[index];

        public int Count => this.items.Count;

        public ValueList<T> Add(T item) {
            return new ValueList<T>(this.items.Add(item));
        }

        public bool Equals(ValueList<T> other) {
            if (this.hashCode != other.hashCode) {
                return false;
            }
            else {
                return this.items.SequenceEqual(other.items);
            }
        }

        public override bool Equals(object? obj) {
            if (obj is ValueList<T> other) {
                return this.Equals(other);
            }

            return false;
        }

        public IEnumerator<T> GetEnumerator() => this.items.GetEnumerator();

        public override int GetHashCode() => this.hashCode;

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

        public static bool operator ==(ValueList<T> list1, ValueList<T> list2) {
            return list1.Equals(list2);
        }

        public static bool operator !=(ValueList<T> list1, ValueList<T> list2) {
            return !list1.Equals(list2);
        }
    }

    public static partial class TypeCheckingExtensions {
        public static ValueList<T> ToValueList<T>(this IEnumerable<T> sequence) => new ValueList<T>(sequence);
    }
}