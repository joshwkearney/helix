using System.Collections;
using System.Collections.Immutable;

namespace Helix.Analysis {
    public class ValueSet<T> : IEquatable<ValueSet<T>>, IEnumerable<T>, IReadOnlySet<T> {
        private readonly int hashCode;
        private readonly ImmutableHashSet<T> items;

        public ValueSet() {
            this.items = ImmutableHashSet<T>.Empty;
            this.hashCode = 0;
        }

        public ValueSet(IEnumerable<T> values) : this(values.ToImmutableHashSet()) { }

        public ValueSet(ImmutableHashSet<T> values) {
            this.items = values;

            int largePrime = 982451653;
            this.hashCode = this.items.Aggregate(1, (x, y) => (x * (y.GetHashCode() % largePrime)) % largePrime);
        }

        public int Count => this.items.Count;

        public ValueSet<T> Add(T item) => new ValueSet<T>(this.items.Add(item));

        public bool Contains(T item) => this.items.Contains(item);

        public IEnumerator<T> GetEnumerator() => this.items.GetEnumerator();

        public bool IsProperSubsetOf(IEnumerable<T> other) => this.items.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other) => this.items.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other) => this.items.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other) => this.items.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other) => this.items.Overlaps(other);

        public bool SetEquals(IEnumerable<T> other) => this.items.SetEquals(other);

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

        public override int GetHashCode() => this.hashCode;

        public override bool Equals(object? obj) {
            if (obj is ValueSet<T> other) {
                return this.Equals(other);
            }

            return false;
        }

        public bool Equals(ValueSet<T> other) {
            if (this.hashCode != other.hashCode) {
                return false;
            }
            else {
                return this.items.SetEquals(other.items);
            }
        }

        public static bool operator ==(ValueSet<T> list1, ValueSet<T> list2) {
            return list1.Equals(list2);
        }

        public static bool operator !=(ValueSet<T> list1, ValueSet<T> list2) {
            return !list1.Equals(list2);
        }
    }

    public static partial class TypeCheckingExtensions {
        public static ValueSet<T> ToValueSet<T>(this IEnumerable<T> sequence) {
            if (sequence is ValueSet<T> set) {
                return set;
            }
            else {
                return new ValueSet<T>(sequence);
            }
        }
    }
}