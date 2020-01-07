using Attempt17.Features.Functions;
using Attempt17.Types;

namespace Attempt17 {
    public class FunctionInfo {
        public FunctionSignature Signature { get; }

        public IdentifierPath Path { get; }

        public NamedType FunctionType => new NamedType(this.Path);

        public FunctionInfo(IdentifierPath path, FunctionSignature sig) {
            this.Path = path;
            this.Signature = sig;
        }
    }
}