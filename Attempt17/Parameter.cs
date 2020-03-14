using System;
using System.Diagnostics.CodeAnalysis;
using Attempt17.Types;

namespace Attempt17 {
    public class Parameter : IEquatable<Parameter> {
        public string Name { get; }

        public LanguageType Type { get; }

        public Parameter(string name, LanguageType type) {
            this.Name = name;
            this.Type = type;
        }

        public override bool Equals(object obj) {
            if (obj is Parameter par) {
                return this.Equals(par);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Name.GetHashCode() + 7 * this.Type.GetHashCode();
        }

        public bool Equals(Parameter other) {
            if (other is null) {
                return false;
            }

            if (this.Name != other.Name) {
                return false;
            }

            if (this.Type != other.Type) {
                return false;
            }

            return true;
        }

        public static bool operator==(Parameter par1, Parameter par2) {
            if (par1 is null) {
                return par2 is null;
            }
            else {
                return par1.Equals(par2);
            }
        }

        public static bool operator !=(Parameter par1, Parameter par2) {
            return !(par1 == par2);
        }
    }
}