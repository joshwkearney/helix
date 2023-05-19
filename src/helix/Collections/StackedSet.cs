using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Collections
{
    public class StackedSet<T> : ISet<T>
    {
        private readonly ISet<T> values = new HashSet<T>();
        private readonly ISet<T> prev;

        public StackedSet(ISet<T> prev)
        {
            this.prev = prev;
        }

        public int Count => values.Count;

        public bool IsReadOnly => values.IsReadOnly;

        public bool Add(T item) => values.Add(item);

        public void Clear() => values.Clear();

        public bool Contains(T item) => values.Contains(item) || prev.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => values.CopyTo(array, arrayIndex);

        public void ExceptWith(IEnumerable<T> other) => values.ExceptWith(other);

        public IEnumerator<T> GetEnumerator() => values.GetEnumerator();

        public void IntersectWith(IEnumerable<T> other) => values.IntersectWith(other);

        public bool IsProperSubsetOf(IEnumerable<T> other) => values.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other) => values.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other) => values.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other) => values.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other) => values.Overlaps(other);

        public bool Remove(T item) => values.Remove(item);

        public bool SetEquals(IEnumerable<T> other) => values.SetEquals(other);

        public void SymmetricExceptWith(IEnumerable<T> other) => values.SymmetricExceptWith(other);

        public void UnionWith(IEnumerable<T> other) => values.UnionWith(other);

        void ICollection<T>.Add(T item) => values.Add(item);

        IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();
    }
}
