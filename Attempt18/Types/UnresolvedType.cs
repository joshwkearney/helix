using System;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt19.Types {
    public class UnresolvedType : LanguageType {
        public IdentifierPath Path { get; }

        public override LanguageTypeKind Kind => LanguageTypeKind.Unresolved;

        public UnresolvedType(IdentifierPath path) {
            this.Path = path;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            return object.ReferenceEquals(this, other);
        }

        public override int GetHashCode() => this.Path.GetHashCode();

        public override string ToString() => this.Path.Segments.Last();

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitUnresolvedType(this);
        }
    }
}