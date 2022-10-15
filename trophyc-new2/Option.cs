using System.Collections;

namespace Trophy {
    public struct Option<T> {
        private readonly T value;

        public bool HasValue { get; }

        public Option(T value) {
            this.value = value;
            this.HasValue = true;
        }

        public Option<E> Select<E>(Func<T, E> selector) {
            if (this.HasValue) {
                return selector(this.value);
            }
                
            return Option.None;
        }

        public Option<E> SelectMany<E>(Func<T, Option<E>> selector) {
            if (this.HasValue) {
                return selector(this.value);
            }

            return Option.None;
        }

        public Option<T> Where(Func<T, bool> filter) {
            if (this.HasValue && filter(this.value)) {
                return this;
            }
                
            return Option.None;
        }

        public IEnumerable<T> ToEnumerable() {
            if (this.HasValue) {
                yield return this.value; 
            }
        }

        public bool TryGetValue(out T value) {
            value = this.value;

            if (this.HasValue) {
                return true;
            }
            else {
                return false;
            }
        }

        public T OrElse(Func<T> supplier) {
            if (this.HasValue) {
                return this.value;
            }
            else {
                return supplier();
            }
        }

        public static implicit operator Option<T>(T value) => new Option<T>(value);

        public static implicit operator Option<T>(Option _) => new Option<T>();
    }

    public class Option {
        public static Option<T> Some<T>(T value) => new Option<T>(value);

        public static Option None { get; } = new Option();

        private Option() { }
    }

    public static class OptionExtensions {
        public static Option<T> FirstOrNone<T>(this IEnumerable<T> seq) {
            if (seq.Any()) {
                return seq.First();
            }
            else {
                return Option.None;
            }
        }
    }
}