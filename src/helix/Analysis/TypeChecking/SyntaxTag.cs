using Helix.Analysis.Predicates;
using Helix.Analysis.Types;

namespace Helix.Analysis.TypeChecking {
    public record SyntaxTag {
        public ISyntaxPredicate Predicate { get; }

        public HelixType ReturnType { get; }
        
        public SyntaxTag(
            HelixType returnType,
            ISyntaxPredicate pred) {

            this.ReturnType = returnType;
            this.Predicate = pred;
        }
    }

}
