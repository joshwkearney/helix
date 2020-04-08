using System;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt18.Types {
    public class StructType : LanguageType {
        public override LanguageTypeKind Kind => LanguageTypeKind.Struct;

        public IdentifierPath Path { get; }

        public StructType(IdentifierPath path) {
            this.Path = path;
        }

        public override bool Equals(object other) {
            if (other is StructType structType) {
                return this.Path == structType.Path;
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Path.GetHashCode();
        }

        public override string ToString() {
            return this.Path.ToString();
        }

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitStructType(this);
        }
    }
}
