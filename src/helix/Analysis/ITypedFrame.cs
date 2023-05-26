using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;
using System.Collections.Immutable;

namespace Helix.Analysis {
    public interface ITypedFrame {
        public ImmutableDictionary<HelixType, HelixType> NominalSupertypes { get; set; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        public IDictionary<IdentifierPath, StructSignature> Unions { get; }

        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public IDictionary<ISyntaxTree, IReadOnlyList<VariableCapture>> CapturedVariables { get; }
    }
}