using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis {
    public class StackedSet<T> : ISet<T> {
        private readonly ISet<T> values = new HashSet<T>();
        private readonly ISet<T> prev;

        public StackedSet(ISet<T> prev) {
            this.prev = prev;
        }

        public int Count => this.values.Count;

        public bool IsReadOnly => this.values.IsReadOnly;

        public bool Add(T item) => this.values.Add(item);

        public void Clear() => this.values.Clear();

        public bool Contains(T item) => this.values.Contains(item) || this.prev.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => this.values.CopyTo(array, arrayIndex);

        public void ExceptWith(IEnumerable<T> other) => this.values.ExceptWith(other);

        public IEnumerator<T> GetEnumerator() => this.values.GetEnumerator();

        public void IntersectWith(IEnumerable<T> other) => this.values.IntersectWith(other);

        public bool IsProperSubsetOf(IEnumerable<T> other) => this.values.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other) => this.values.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other) => this.values.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other) => this.values.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other) => this.values.Overlaps(other);

        public bool Remove(T item) => this.values.Remove(item);

        public bool SetEquals(IEnumerable<T> other) => this.values.SetEquals(other);

        public void SymmetricExceptWith(IEnumerable<T> other) => this.values.SymmetricExceptWith(other);

        public void UnionWith(IEnumerable<T> other) => this.values.UnionWith(other);

        void ICollection<T>.Add(T item) => this.values.Add(item);

        IEnumerator IEnumerable.GetEnumerator() => this.values.GetEnumerator();
    }
}
