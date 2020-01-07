using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt17 {
    public interface IOption<out T> {
        E Match<E>(Func<T, E> ifsome, Func<E> ifnone);
    }

    public static class Option {
        public static IOption<T> Some<T>(T value) => new OptionSome<T>(value);

        public static IOption<T> None<T>() => OptionNone<T>.Instance;

        public static IOption<E> Select<T, E>(this IOption<T> option, Func<T, E> selector) {
            return option.Match(x => Some(selector(x)), () => None<E>());
        }

        public static IOption<E> SelectMany<T, E>(this IOption<T> option, Func<T, IOption<E>> selector) {
            return option.Match(selector, () => None<E>());
        }

        public static IOption<T> Where<T>(this IOption<T> option, Func<T, bool> predicate) {
            return option.Match(x => predicate(x) ? option : None<T>(), () => None<T>());
        }

        public static bool Any<T>(this IOption<T> option) {
            return option.Match(_ => true, () => false);
        }

        public static bool TryGetValue<T>(this IOption<T> option, out T value) {
            if (option.Any()) {
                value = option.Match(x => x, () => default);
                return true;
            }
            else {
                value = default;
                return false;
            }
        }

        public static T GetValue<T>(this IOption<T> option) {
            if (option.TryGetValue(out T t)) {
                return t;
            }
            else {
                throw new Exception();
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this IOption<T> option) {
            return option.Match(x => new[] { x }, () => Enumerable.Empty<T>());
        }

        private class OptionNone<T> : IOption<T> {
            public static OptionNone<T> Instance { get; } = new OptionNone<T>();

            public E Match<E>(Func<T, E> ifsome, Func<E> ifnone) => ifnone();
        }

        private class OptionSome<T> : IOption<T> {
            public T Value { get; }

            public OptionSome(T value) {
                this.Value = value;
            }

            public E Match<E>(Func<T, E> ifsome, Func<E> ifnone) => ifsome(this.Value);
        }
    }
}