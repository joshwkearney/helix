using System.Linq;
using Attempt17.NewSyntax;

namespace Attempt18.Types {
    public class NamedType : LanguageType {
        public IdentifierPath Path { get; }

        public NamedType(IdentifierPath path) {
            this.Path = path;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is NamedType named) {
                return this.Path == named.Path;
            }

            return false;
        }

        public override int GetHashCode() => this.Path.GetHashCode();

        public override string ToString() => this.Path.Segments.Last();

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitNamedType(this);
        }

        public override string ToFriendlyString() {
            throw new System.NotImplementedException();
        }

        public override TypeCopiability GetCopiability() {
            throw new System.NotImplementedException();
        }
    }
}