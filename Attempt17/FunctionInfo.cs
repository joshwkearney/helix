using Attempt17.Types;

namespace Attempt17 {
    public class FunctionInfo : IIdentifierTarget {
        public FunctionSignature Signature { get; }

        public IdentifierPath Path { get; }

        public LanguageType Type => new NamedType(this.Path);

        public FunctionInfo(IdentifierPath path, FunctionSignature sig) {
            this.Path = path;
            this.Signature = sig;
        }

        public T Accept<T>(IIdentifierTargetVisitor<T> visitor) {
            return visitor.VisitFunction(this);
        }
    }
}