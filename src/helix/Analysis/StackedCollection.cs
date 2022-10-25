//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Helix.Analysis {
//    public class StackedCollection<T> : ICollection<T> {
//        private readonly ICollection<T> prev;
//        private readonly HashSet<T> values = new();

//        public StackedCollection(ICollection<T> prev) {
//            this.prev = prev;
//        }

//        public int Count => this.values.Count;

//        public bool IsReadOnly => false;

//        public void Add(T item) => this.values.Add(item);

//        public void Clear() => this.values.Clear();

//        public bool Contains(T item) {
//            return this.values.Contains(item) || this.prev.Contains(item);
//        }

//        public void CopyTo(T[] array, int arrayIndex) {
//            this.values.CopyTo(array, arrayIndex);
//        }

//        public IEnumerator<T> GetEnumerator() => this.values.GetEnumerator();

//        public bool Remove(T item) => this.values.Remove(item);

//        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
//    }
//}
