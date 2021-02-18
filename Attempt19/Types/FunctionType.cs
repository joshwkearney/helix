using System.Linq;

namespace Attempt19.Types {
    public class FunctionType : LanguageType {
        public IdentifierPath FunctionPath { get; }

        public FunctionType(IdentifierPath path) {
            this.FunctionPath = path;
        }

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitFunctionType(this);
        }

        public override bool Equals(object other) {
            return other is FunctionType func && this.FunctionPath == func.FunctionPath;
        }

        public override TypeCopiability GetCopiability() {
            return TypeCopiability.Unconditional;
        }

        public override int GetHashCode() {
            return this.FunctionPath.GetHashCode();
        }

        public override string ToFriendlyString() {
            return this.FunctionPath.Segments.Last();
        }

        public override string ToString() {
            return this.FunctionPath.ToString();
        }
    }
}