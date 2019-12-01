using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12.TypeSystem {
    public class RootSymbol : ISymbol {
        public static ISymbol Instance { get; } = new RootSymbol();

        public ISymbol BaseType => this;

        private RootSymbol() { }

        public override string ToString() {
            return "root";
        }
    }
}