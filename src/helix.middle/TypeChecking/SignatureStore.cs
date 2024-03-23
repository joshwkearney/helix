using Helix.Common.Types;

namespace Helix.MiddleEnd.TypeChecking {
    internal class SignatureStore {
        public Dictionary<IHelixType, FunctionSignature> FunctionSignatures { get; } = [];

        public Dictionary<IHelixType, StructSignature> StructSignatures { get; } = [];

        public Dictionary<IHelixType, UnionSignature> UnionSignatures { get; } = [];
    }
}
