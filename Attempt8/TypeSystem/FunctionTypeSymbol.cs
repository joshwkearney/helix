using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt12.TypeSystem {
    public class FunctionTypeSymbol : ISymbol, IEquatable<FunctionTypeSymbol> {       
        public ISymbol BaseType => RootSymbol.Instance;

        public ISymbol ReturnType { get; }

        public ImmutableList<ISymbol> ParameterTypes { get; }

        public FunctionTypeSymbol(ISymbol returnType, IEnumerable<ISymbol> paramTypes) {
            this.ReturnType = returnType;
            this.ParameterTypes = paramTypes.ToImmutableList();
        }

        public FunctionTypeSymbol(ISymbol returnType, params ISymbol[] paramTypes)
            : this(returnType, (IEnumerable<ISymbol>)paramTypes) { }

        public bool Equals(FunctionTypeSymbol other) {
            if (!this.ReturnType.Equals(other.ReturnType)) {
                return false;
            }

            if (!this.ParameterTypes.SequenceEqual(other.ParameterTypes)) {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj) {
            if (obj is FunctionTypeSymbol functype) {
                return this.Equals(functype);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.ParameterTypes
                .Concat(new[] { this.ReturnType })
                .Aggregate(17, (x, y) => x + 97 * y.GetHashCode());
        }

        public override string ToString() {
            return $"({string.Join("*", this.ParameterTypes)}->{this.ReturnType})";
        }
    }
}