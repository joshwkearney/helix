using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt19.Types {
    public class ArrayType : LanguageType {
        public LanguageType ElementType { get; }

        public override LanguageTypeKind Kind => LanguageTypeKind.Array;

        public ArrayType(LanguageType elemType) {
            this.ElementType = elemType;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is ArrayType arrType) {
                return this.ElementType == arrType.ElementType;
            }

            return false;
        }

        public override int GetHashCode() {
            return ElementType.GetHashCode();
        }

        public override string ToString() {
            return this.ElementType.ToString() + "[]";
        }

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitArrayType(this);
        }
    }
}