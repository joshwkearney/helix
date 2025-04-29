using System.Collections;
using System.Collections.Immutable;

namespace Helix.Collections;

public class ValueSet<T> : IEquatable<ValueSet<T>>, IEnumerable<T>, IReadOnlySet<T>, 
    IReadOnlyCollection<T>, IImmutableSet<T> {

    private readonly int hashCode;
    private readonly IImmutableSet<T> items;

    public ValueSet() : this(ImmutableHashSet<T>.Empty) { }

    public ValueSet(IEnumerable<T> values) : this(values.ToImmutableHashSet()) { }

    public ValueSet(IImmutableSet<T> values)
        : this(values, values.Aggregate(982451653, (x, y) => x + y?.GetHashCode() ?? 0)) { }

    private ValueSet(IImmutableSet<T> values, int hash) {
        this.items = values;
        this.hashCode = hash;
    }

    public int Count => this.items.Count;

    public ValueSet<T> Add(T item) => new(this.items.Add(item), this.hashCode + item?.GetHashCode() ?? 0);

    public ValueSet<T> Remove(T item) => new(this.items.Remove(item), this.hashCode - item?.GetHashCode() ?? 0);

    public ValueSet<T> Except(IEnumerable<T> values) => new(this.items.Except(values));

    public ValueSet<T> SymmetricExcept(IEnumerable<T> values) => new(this.items.SymmetricExcept(values));

    public ValueSet<T> Union(IEnumerable<T> values) => new(this.items.Union(values));

    public ValueSet<T> Intersect(IEnumerable<T> values) => new(this.items.Intersect(values));

    public ValueSet<T> Clear() => new();

    public bool Contains(T item) => this.items.Contains(item);

    public bool TryGetValue(T equalValue, out T actualValue) => this.items.TryGetValue(equalValue, out actualValue);

    public IEnumerator<T> GetEnumerator() => this.items.GetEnumerator();

    public bool IsProperSubsetOf(IEnumerable<T> other) => this.items.IsProperSubsetOf(other);

    public bool IsProperSupersetOf(IEnumerable<T> other) => this.items.IsProperSupersetOf(other);

    public bool IsSubsetOf(IEnumerable<T> other) => this.items.IsSubsetOf(other);

    public bool IsSupersetOf(IEnumerable<T> other) => this.items.IsSupersetOf(other);

    public bool Overlaps(IEnumerable<T> other) => this.items.Overlaps(other);

    public bool SetEquals(IEnumerable<T> other) => this.items.SetEquals(other);

    IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

    IImmutableSet<T> IImmutableSet<T>.Add(T value) => this.Add(value);

    IImmutableSet<T> IImmutableSet<T>.Clear() => this.Clear();

    IImmutableSet<T> IImmutableSet<T>.Except(IEnumerable<T> other) => this.Except(other);

    IImmutableSet<T> IImmutableSet<T>.Intersect(IEnumerable<T> other) => this.Intersect(other);

    IImmutableSet<T> IImmutableSet<T>.Remove(T value) => this.Remove(value);

    IImmutableSet<T> IImmutableSet<T>.SymmetricExcept(IEnumerable<T> other) => this.SymmetricExcept(other);

    IImmutableSet<T> IImmutableSet<T>.Union(IEnumerable<T> other) => this.Union(other);

    public override int GetHashCode() => this.hashCode;

    public override bool Equals(object? obj) {
        if (obj is ValueSet<T> other) {
            return this.Equals(other);
        }

        return false;
    }

    public bool Equals(ValueSet<T>? other) {
        if (other is null) {
            return false;
        }
        
        if (this.hashCode != other.hashCode) {
            return false;
        }
            
        return this.items.SetEquals(other.items);
    }

    public static bool operator ==(ValueSet<T> list1, ValueSet<T> list2) {
        return list1.Equals(list2);
    }

    public static bool operator !=(ValueSet<T> list1, ValueSet<T> list2) {
        return !list1.Equals(list2);
    }
}