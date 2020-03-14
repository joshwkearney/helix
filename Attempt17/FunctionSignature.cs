using Attempt17.Types;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Attempt17 {
    public class FunctionSignature : IEquatable<FunctionSignature> {
        public LanguageType ReturnType { get; }

        public ImmutableList<Parameter> Parameters { get; }

        public string Name { get; }

        public FunctionSignature(string name, LanguageType returnType, ImmutableList<Parameter> pars) {
            this.Name = name;
            this.ReturnType = returnType;
            this.Parameters = pars;
        }

        public override bool Equals(object obj) {
            if (obj is FunctionSignature sig) {
                return this.Equals(sig);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Name.GetHashCode()
                + 7 * this.ReturnType.GetHashCode()
                + 11 * this.Parameters.Aggregate(23, (x, y) => x + 101 * y.GetHashCode());
        }

        public bool Equals(FunctionSignature other) {
            if (other is null) {
                return false;
            }

            if (this.Name != other.Name) {
                return false;
            }

            if (other.ReturnType != this.ReturnType) {
                return false;
            }

            if (!this.Parameters.SequenceEqual(other.Parameters)) {
                return false;
            }

            return true;
        }

        public static bool operator ==(FunctionSignature sig1, FunctionSignature sig2) {
            if (sig1 is null) {
                return sig2 is null;
            }

            return sig1.Equals(sig2);
        }

        public static bool operator !=(FunctionSignature sig1, FunctionSignature sig2) {
            return !(sig1 == sig2);
        }
    }
}