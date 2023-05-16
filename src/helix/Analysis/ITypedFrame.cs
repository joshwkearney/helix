using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using Helix.Features.Functions;

namespace Helix.Analysis {
    public interface ITypedFrame {
        public IDictionary<IdentifierPath, VariableSignature> Variables { get; }

        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }
    }
}