using System.Collections.Generic;
using System.Linq;

namespace Attempt7 {
    public class Scope {
        public static Scope GlobalScope { get; } = new Scope(new Dictionary<string, ISymbol>() {
            { "int32", LanguageType.Int32Type }
        });

        public IReadOnlyDictionary<string, ISymbol> Variables { get; }

        public Scope() {
            this.Variables = new Dictionary<string, ISymbol>();
        }

        public Scope(IReadOnlyDictionary<string, ISymbol> dict) {
            this.Variables = dict;
        }

        public Scope(IEnumerable<KeyValuePair<string, ISymbol>> dict) {
            this.Variables = dict.ToDictionary(x => x.Key, x => x.Value);
        }

        public Scope WithVariable(string name, ISymbol value) {
            var mutable = this.Variables.ToDictionary(x => x.Key, x => x.Value);
            mutable[name] = value;

            return new Scope(mutable);
        }
    }
}