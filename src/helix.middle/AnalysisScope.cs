using Helix.MiddleEnd.Interpreting;
using Helix.MiddleEnd.TypeChecking;

namespace Helix.MiddleEnd {
    internal readonly struct AnalysisScope {
        public TypeStore Types { get; private init; }

        public AliasStore Aliases { get; private init; }

        public PredicateStore Predicates { get; private init; }

        public AnalysisScope(AnalysisContext context) {
            this.Types = new TypeStore(context);
            this.Aliases = new AliasStore(context);
            this.Predicates = new PredicateStore(context);
        }

        public AnalysisScope CreateScope() {
            return new AnalysisScope() {
                Aliases = this.Aliases.CreateScope(),
                Predicates = this.Predicates.CreateScope(),
                Types = this.Types.CreateScope()
            };
        }

        public bool WasModifiedBy(AnalysisScope other) {
            return this.Types.WasModifiedBy(other.Types) 
                || this.Aliases.WasModifiedBy(other.Aliases) 
                || this.Predicates.WasModifiedBy(other.Predicates);
        }

        public AnalysisScope MergeWith(AnalysisScope other) {
            return new AnalysisScope() {
                Aliases = this.Aliases.MergeWith(other.Aliases),
                Predicates = this.Predicates.MergeWith(other.Predicates),
                Types = this.Types.MergeWith(other.Types)
            };
        }
    }
}
