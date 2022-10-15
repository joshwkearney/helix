using Trophy.Features.Aggregates;
using Trophy.Features.Functions;

namespace Trophy.Analysis.SyntaxTree {
    public class TypesRecorder {
        public Dictionary<IdentifierPath, TrophyType> Variables { get; } = new();

        public Dictionary<IdentifierPath, FunctionSignature> Functions { get; } = new();

        public Dictionary<IdentifierPath, AggregateSignature> Aggregates { get; } = new();
    }
}
