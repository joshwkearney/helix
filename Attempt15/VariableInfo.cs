using JoshuaKearney.Attempt15.Types;
using System;

namespace JoshuaKearney.Attempt15 {
    public struct VariableInfo : IEquatable<VariableInfo> {
        public string Name { get; }

        public ITrophyType Type { get; }

        public bool IsImmutable { get; }

        public VariableInfo(string name, ITrophyType type, bool isImmutable) {
            this.Name = name;
            this.Type = type;
            this.IsImmutable = isImmutable;
        }

        public bool Equals(VariableInfo other) {
            return this.Name == other.Name && this.Type.Equals(other.Type) && this.IsImmutable == other.IsImmutable;
        }

        public override bool Equals(object obj) {
            if (obj is IdentifierInfo info) {
                return this.Equals(info);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Name.GetHashCode() + 7 * this.Type.GetHashCode() + 21 * this.IsImmutable.GetHashCode();
        }

        public IdentifierInfo ToIdentifierInfo() => new IdentifierInfo(this.Name, this.Type);
    }       
}