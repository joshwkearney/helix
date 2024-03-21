using Helix.Common.Collections;
using System.Collections.Immutable;

namespace Helix.Common {
    public static class Extensions {        
        public static ValueSet<T> ToValueSet<T>(this IEnumerable<T> sequence) {
            if (sequence is ValueSet<T> list) {
                return list;
            }
            else if (sequence is IImmutableSet<T> immList) {
                return new ValueSet<T>(immList);
            }
            else {
                return new ValueSet<T>(sequence);
            }
        }
    }
}
