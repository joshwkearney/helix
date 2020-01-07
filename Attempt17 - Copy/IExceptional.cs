//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Attempt17 {
//    public interface IOption<out T> {
//        public TResult Match<TResult>(Func<T, TResult> ifsome, Func<Exception, TResult> iferror);
//    }

//    public static class Option {
//        public static IOption<T> Some<T>(T data) {
//            return new OptionSome<T>(data);
//        }

//        public static IOption<T> None<T>(Exception error) {
//            return new OptionError<T>(error);
//        }

//        private class OptionSome<T> : IOption<T> {
//            private readonly T data;

//            public OptionSome(T data) {
//                this.data = data;
//            }

//            public TResult Match<TResult>(Func<T, TResult> ifsome, Func<Exception, TResult> iferror) {
//                return ifsome(this.data);
//            }
//        }

//        private class OptionError<T> : IOption<T> {
//            private readonly Exception data;

//            public OptionError(Exception data) {
//                this.data = data;
//            }

//            public TResult Match<TResult>(Func<T, TResult> ifsome, Func<Exception, TResult> iferror) {
//                return iferror(this.data);
//            }
//        }
//    }

//    public static class OptionExtensions {
//        public static bool HasError<T>(this IOption<T> option) {
//            return option.Match(x => false, x => true);
//        }

//        public static T GetValue<T>(this IOption<T> option) {
//            if (option.HasError()) {
//                throw option.Match(x => new Exception("This should never happen"), x => x);
//            }
//            else {
//                return option.Match(x => x, x => default);
//            }
//        }

//        public static IOption<T> Where<T>(this IOption<T> option, Func<T, bool> predicate, Func<T, Exception> iferror) {
//            return option.Match(
//                x => predicate(x) ? Option.Some(x) : Option.None<T>(iferror(x)),
//                x => Option.None<T>(x));
//        }

//        public static IOption<E> Select<T, E>(this IOption<T> option, Func<T, E> selector) {
//            return option.Match(x => Option.Some(selector(x)), x => Option.None<E>(x));
//        }

//        public static IOption<E> SelectMany<T, E>(this IOption<T> option, Func<T, IOption<E>> selector) {
//            return option.Match(selector, x => Option.None<E>(x));
//        }
//    }
//}