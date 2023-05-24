using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix {
    public static class Extensions {
        public static int IndexOf<T>(this IReadOnlyList<T> list, Func<T, bool> selector) {
            for (int i = 0; i < list.Count; i++) {
                if (selector(list[i])) {
                    return i;
                }
            }

            return -1;
        }
    }
}
