using System.Collections.Generic;
using System.Linq;

namespace Attempt10 {
    public class FunctionTrophyType : ITrophyType {
        public IReadOnlyList<ITrophyType> ParameterTypes { get; }

        public ITrophyType ReturnType { get; }

        public FunctionTrophyType(ITrophyType returnType, IReadOnlyList<ITrophyType> pars) {
            this.ReturnType = returnType;
            this.ParameterTypes = pars;
        }

        public void Accept(ITrophyTypeVisitor visitor) => visitor.Visit(this);

        public bool Equals(ITrophyType other) {
            if (!(other is FunctionTrophyType funcType)) {
                return false;
            }

            if (!funcType.ReturnType.Equals(this.ReturnType)) {
                return false;
            }

            if (!this.ParameterTypes.SequenceEqual(funcType.ParameterTypes)) {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj) {
            if (obj is ITrophyType type) {
                return this.Equals(type);
            }

            return false;
        }

        public override int GetHashCode() {
            var hashCode = -1063252000;
            hashCode = hashCode * -1521134295 + this.ParameterTypes.GetHashCode();
            hashCode = hashCode * -1521134295 + this.ReturnType.GetHashCode();
            return hashCode;
        }

        public bool IsCompatibleWith(ITrophyType other) => this.Equals(other);
    }

    public class ClosureTrophyType : ITrophyType {
        public FunctionTrophyType FunctionType { get; }

        public ClosureTrophyType(FunctionTrophyType abstractType) {
            this.FunctionType = abstractType;
        }

        public void Accept(ITrophyTypeVisitor visitor) => visitor.Visit(this);

        public bool Equals(ITrophyType other) {
            if (!(other is ClosureTrophyType funcType)) {
                return false;
            }

            if (this.FunctionType != funcType.FunctionType) {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj) {
            if (obj is ITrophyType type) {
                return this.Equals(type);
            }

            return false;
        }

        public override int GetHashCode() => this.FunctionType.GetHashCode();

        public bool IsCompatibleWith(ITrophyType other) {
            if (this.Equals(other)) {
                return true;
            }

            if (!(other is FunctionTrophyType func)) {
                return false;
            }

            return this.FunctionType.Equals(func);
        }
    }    
}