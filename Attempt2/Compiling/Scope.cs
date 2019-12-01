using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt2.Compiling {
    public class Scope {
        public IReadOnlyDictionary<string, ISymbol> Symbols { get; }

        public Scope() {
            this.Symbols = new Dictionary<string, ISymbol>();
        }

        public Scope(IReadOnlyDictionary<string, ISymbol> dict) {
            this.Symbols = dict;
        }

        public Scope AddSymbol(string name, ISymbol symbol) {
            var syms = this.Symbols.ToDictionary(x => x.Key, x => x.Value);
            syms.Add(name, symbol);

            return new Scope(syms);
        }
    }
}