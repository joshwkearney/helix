using System;
using System.Collections.Immutable;
using System.Linq;

namespace Trophy.Features.Functions {
    public class FunctionSignature : IEquatable<FunctionSignature> {
        public TrophyType ReturnType { get; }

        public ImmutableList<FunctionParameter> Parameters { get; }

        public IdentifierPath Path { get; }

        public FunctionSignature(IdentifierPath path, TrophyType returnType, ImmutableList<FunctionParameter> pars) {
            this.Path = path;
            this.ReturnType = returnType;
            this.Parameters = pars;
        }

        public override bool Equals(object? obj) {
            if (obj is FunctionSignature sig) {
                return this.Equals(sig);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Path.GetHashCode()
                + 7 * this.ReturnType.GetHashCode()
                + 11 * this.Parameters.Aggregate(23, (x, y) => x + 101 * y.GetHashCode());
        }

        public bool Equals(FunctionSignature? other) {
            if (other is null) {
                return false;
            }

            if (this.Path != other.Path) {
                return false;
            }

            if (!other.ReturnType.Equals(this.ReturnType)) {
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

    public class FunctionParameter : IEquatable<FunctionParameter> {
        public string Name { get; }

        public TrophyType Type { get; }

        public FunctionParameter(string name, TrophyType type) {
            this.Name = name;
            this.Type = type;
        }

        public override bool Equals(object? obj) {
            if (obj is FunctionParameter par) {
                return this.Equals(par);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Name.GetHashCode() + 7 * this.Type.GetHashCode();
        }

        public bool Equals(FunctionParameter? other) {
            if (other is null) {
                return false;
            }


            if (this.Name != other.Name) {
                return false;
            }

            if (!this.Type.Equals(other.Type)) {
                return false;
            }

            return true;
        }

        public static bool operator ==(FunctionParameter par1, FunctionParameter par2) {
            if (par1 is null) {
                return par2 is null;
            }
            else {
                return par1.Equals(par2);
            }
        }

        public static bool operator !=(FunctionParameter par1, FunctionParameter par2) {
            return !(par1 == par2);
        }
    }
}