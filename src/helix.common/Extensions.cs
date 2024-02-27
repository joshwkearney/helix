using Helix.Analysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Collections {
    public static class Extensions {
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
