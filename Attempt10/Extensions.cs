using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12 {
    public static class Extensions {
        public static StringBuilder Append(this StringBuilder sb, char c, int count) {
            return sb.Append(new string(c, count));
        }
    }
}