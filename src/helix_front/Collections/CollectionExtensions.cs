using Helix.Analysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Collections {
    public static class CollectionExtensions {
        public static IDictionary<TKey, TValue> ToDefaultDictionary<TKey, TValue>(
            this IDictionary<TKey, TValue> dict, 
            Func<TKey, TValue> valueFactory) {

            return new DefaultDictionary<TKey, TValue>(dict, valueFactory);
        }

        public static ValueSet<T> ToValueSet<T>(this IEnumerable<T> sequence) {
            if (sequence is ValueSet<T> set) {
                return set;
            }
            else if (sequence is IImmutableSet<T> immSet) {
                return new ValueSet<T>(immSet);
            }
            else {
                return new ValueSet<T>(sequence);
            }
        }

        public static ValueList<T> ToValueList<T>(this IEnumerable<T> sequence) {
            if (sequence is ValueList<T> list) {
                return list;
            }
            else if (sequence is IImmutableSet<T> immList) {
                return new ValueList<T>(immList);
            }
            else {
                return new ValueList<T>(sequence);
            }
        }
    }
}
