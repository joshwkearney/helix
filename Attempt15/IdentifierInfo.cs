using JoshuaKearney.Attempt15.Types;
using System;

namespace JoshuaKearney.Attempt15 {
    public struct IdentifierInfo : IEquatable<IdentifierInfo> {
        public string Name { get; }

        public ITrophyType Type { get; }

        public IdentifierInfo(string name, ITrophyType type) {
            this.Name = name;
            this.Type = type;
        }

        public bool Equals(IdentifierInfo other) {
            return this.Name == other.Name && this.Type.Equals(other.Type);
        }

        public override bool Equals(object obj) {
            if (obj is IdentifierInfo info) {
                return this.Equals(info);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Name.GetHashCode() + 7 * this.Type.GetHashCode();
        }

        public VariableInfo ToVariableInfo(bool isImmutable) {
            return new VariableInfo(this.Name, this.Type, isImmutable);
        }
    }       
}