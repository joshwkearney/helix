using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt6 {
    public class AssociativeList<T1, T2> : IEnumerable<KeyValuePair<T1, T2>>, IReadOnlyCollection<KeyValuePair<T1, T2>>, IReadOnlyList<KeyValuePair<T1, T2>> {
        public static AssociativeList<T1, T2> Empty { get; } = new AssociativeList<T1, T2>();

        public AssociativeList<T1, T2> Next { get; }

        private KeyValuePair<T1, T2> Data { get; }

        public T1 Key => this.Data.Key;

        public T2 Value => this.Data.Value;

        public int Count { get; }

        public KeyValuePair<T1, T2> this[int index] {
            get {
                if (index == 0) {
                    return this.Data;
                }
                else if (index < 0 || index >= this.Count || this == null) {
                    throw new IndexOutOfRangeException();
                }
                else {
                    return this.Next[index - 1];
                }
            }
        }

        public T2 this[T1 index] {
            get {
                if (this.TryGetValue(index, out var value)) {
                    return value;
                }
                else {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        public AssociativeList(KeyValuePair<T1, T2> data) : this(data, Empty) { }

        public AssociativeList(T1 key, T2 value) : this(new KeyValuePair<T1, T2>(key, value), Empty) { }

        private AssociativeList(KeyValuePair<T1, T2> data, AssociativeList<T1, T2> next) {
            this.Data = data;
            this.Next = next;
            this.Count = 1 + (next == null ? 0 : next.Count);
        }

        private AssociativeList(T1 key, T2 value, AssociativeList<T1, T2> next) : this(new KeyValuePair<T1, T2>(key, value), next) { }

        private AssociativeList() {
            this.Data = default;
            this.Next = null;
            this.Count = 0;
        }

        public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator() => new AssociativeListEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public AssociativeList<T1, T2> Append(KeyValuePair<T1, T2> data) {
            return new AssociativeList<T1, T2>(data, this);
        }

        public AssociativeList<T1, T2> Append(T1 key, T2 value) {
            return new AssociativeList<T1, T2>(key, value, this);
        }

        public AssociativeList<T1, T2> AppendAll(IEnumerable<KeyValuePair<T1, T2>> data) {
            return data.Aggregate(this, (x, y) => x.Append(y));
        }

        public bool TryGetValue(T1 key, out T2 value) {
            if (this.Data.Key.Equals(key)) {
                value = this.Data.Value;
                return true;
            }
            else if (this.Next == Empty) {
                value = default;
                return false;
            }
            else {
                return this.Next.TryGetValue(key, out value);
            }
        }

        public bool Contains(T1 key) {
            return this.TryGetValue(key, out _);
        }

        private class AssociativeListEnumerator : IEnumerator<KeyValuePair<T1, T2>> {
            private readonly AssociativeList<T1, T2> original;
            private AssociativeList<T1, T2> current = null;

            public KeyValuePair<T1, T2> Current => this.current?.Data ?? default;

            object IEnumerator.Current => this.Current;

            public AssociativeListEnumerator(AssociativeList<T1, T2> list) {
                this.original = list;
            }

            public void Dispose() { }

            public bool MoveNext() {
                if (this.current == null) {
                    this.current = this.original;
                    return true;
                }
                else if (this.current == Empty) {
                    return false;
                }
                else if (this.current.Next == Empty) {
                    this.current = Empty;
                    return false;
                }
                else {
                    this.current = this.current.Next;
                    return true;
                }
            }

            public void Reset() {
                this.current = null;
            }
        }
    }
}