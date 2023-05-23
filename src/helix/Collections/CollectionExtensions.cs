using Helix.Analysis;
using System;
using System.Collections.Generic;
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
            else {
                return new ValueSet<T>(sequence);
            }
        }
    }
}
