using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt4 {
    public class Scope {
        public IReadOnlyDictionary<string, IAnalyzedSyntax> Symbols { get; }

        public Scope() {
            this.Symbols = new Dictionary<string, IAnalyzedSyntax>();
        }

        public Scope(IReadOnlyDictionary<string, IAnalyzedSyntax> dict) {
            this.Symbols = dict;
        }

        public Scope AddSymbol(string name, IAnalyzedSyntax symbol) {
            var syms = this.Symbols.ToDictionary(x => x.Key, x => x.Value);
            syms.Add(name, symbol);

            return new Scope(syms);
        }
    }
}