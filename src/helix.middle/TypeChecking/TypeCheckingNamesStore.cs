using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeCheckingNamesStore {
        private int counter = 0;

        public string GetConvertName() {
            return "__convert_" + this.counter++;
        }
    }
}
