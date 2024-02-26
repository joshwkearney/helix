using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace helix.common {
    public record Unit {
        public static Unit Instance { get; } = new();
    }
}
