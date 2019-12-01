using System.Collections.Generic;
using System.Linq;

namespace Attempt12 {
    public class TrophyFunctionType : ITrophyType {
        public IReadOnlyList<ITrophyType> ParameterTypes { get; }

        public ITrophyType ReturnType { get; }

        public bool IsLiteral => false;

        public bool IsMovable => true;

        public TrophyFunctionType(ITrophyType returnType, IReadOnlyList<ITrophyType> pars) {
            this.ReturnType = returnType;
            this.ParameterTypes = pars;
        }

        public void Accept(ITrophyTypeVisitor visitor) => visitor.Visit(this);

        public bool Equals(ITrophyType other) {
            if (!(other is TrophyFunctionType funcType)) {
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

        public bool IsCompatibleWith(ITrophyType other) {
            if (this.Equals(other)) {
                return true;
            }

            if (!(other is TrophyFunctionType func)) {
                return false;
            }

            if (!this.ReturnType.Equals(func.ReturnType)) {
                return false;
            }

            if (!this.ParameterTypes.SequenceEqual(func.ParameterTypes)) {
                return false;
            }

            return true;
        }
    }    
}