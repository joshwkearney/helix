using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12.TypeSystem {
    public static class PrimitiveTypes {
        public static ISymbol Int32Type { get; } = new Symbol("int32");

        public static ISymbol Float32Type { get; } = new Symbol("float32");

        private class Symbol : ISymbol {
            private readonly string name;

            public ISymbol BaseType => RootSymbol.Instance;

            public Symbol(string name) {
                this.name = name;
            }

            public override string ToString() {
                return this.name;
            }
        }
    }
}