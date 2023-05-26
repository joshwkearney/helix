using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;
using System.Collections.Immutable;

namespace Helix.Analysis {
    public interface ITypedFrame {
        public ImmutableDictionary<IdentifierPath, ISyntaxTree> SyntaxValues { get; set; }

        public ImmutableDictionary<IdentifierPath, HelixType> NominalSignatures { get; set; }

        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public IDictionary<ISyntaxTree, IReadOnlyList<VariableCapture>> CapturedVariables { get; }
    }
}