using System;
using System.Collections.Immutable;

namespace Attempt19.Types {
    public class FunctionType : LanguageType {
        public IdentifierPath FunctionPath { get; }

        public FunctionType(IdentifierPath path) {
            this.FunctionPath = path;
        }

        public override LanguageTypeKind Kind => LanguageTypeKind.Function;

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is FunctionType funcType) {
                return this.FunctionPath == funcType.FunctionPath;
            }

            return false;
        }

        public override int GetHashCode() => this.FunctionPath.GetHashCode();

        public override string ToString() {
            throw new NotImplementedException();
        }

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitFunctionType(this);
        }
    }
}
