using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace helix.common {
    public sealed class Assert {
        public static void IsTrue(bool value) {
            if (!value) {
                throw new InvalidOperationException("Assertion failed!");
            }
        }

        public static void IsFalse(bool value) {
            if (value) {
                throw new InvalidOperationException("Assertion failed!");
            }
        }
    }
}
