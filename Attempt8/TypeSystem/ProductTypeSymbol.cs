using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt12.TypeSystem {
    public class ProductTypeSymbol : ISymbol, IEquatable<ProductTypeSymbol> {
        public ISymbol BaseType => RootSymbol.Instance;

        public ImmutableList<ISymbol> FactorTypes { get; }

        public ProductTypeSymbol(IEnumerable<ISymbol> paramTypes) {
            this.FactorTypes = paramTypes.ToImmutableList();
        }

        public ProductTypeSymbol(params ISymbol[] paramTypes)
            : this((IEnumerable<ISymbol>)paramTypes) { }

        public bool Equals(ProductTypeSymbol other) {
            if (!this.FactorTypes.SequenceEqual(other.FactorTypes)) {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj) {
            if (obj is ProductTypeSymbol functype) {
                return this.Equals(functype);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.FactorTypes
                .Aggregate(17, (x, y) => x + 97 * y.GetHashCode());
        }

        public override string ToString() {
            return $"({string.Join("*", this.FactorTypes)})";
        }
    }
}