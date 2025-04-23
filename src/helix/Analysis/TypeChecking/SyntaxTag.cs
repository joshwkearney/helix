using Helix.Analysis.Predicates;
using Helix.Analysis.Types;

namespace Helix.Analysis.TypeChecking {
    public record SyntaxTag {
        public IReadOnlyList<VariableCapture> CapturedVariables { get; }

        public ISyntaxPredicate Predicate { get; }

        public HelixType ReturnType { get; }
        
        public SyntaxTag(
            HelixType returnType,
            IReadOnlyList<VariableCapture> cap, 
            ISyntaxPredicate pred) {

            this.ReturnType = returnType;
            this.CapturedVariables = cap;
            this.Predicate = pred;
        }
    }

}
