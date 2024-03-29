﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix {
    public static class Extensions {
        public static int IndexOf<T>(this IEnumerable<T> list, Func<T, bool> selector) {
            int i = 0;

            foreach (var item in list) { 
                if (selector(item)) {
                    return i;
                }

                i++;
            }

            return -1;
        }
    }
}
