using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt3 {
    public class Scope {
        public IReadOnlyDictionary<string, IValue> Symbols { get; }

        public Scope() {
            this.Symbols = new Dictionary<string, IValue>();
        }

        public Scope(IReadOnlyDictionary<string, IValue> dict) {
            this.Symbols = dict;
        }

        public Scope AddSymbol(string name, IValue symbol) {
            var syms = this.Symbols.ToDictionary(x => x.Key, x => x.Value);
            syms.Add(name, symbol);

            return new Scope(syms);
        }
    }
}