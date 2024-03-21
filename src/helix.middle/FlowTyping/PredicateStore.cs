using Helix.Common;
using Helix.Common.Types;
using Helix.MiddleEnd.FlowTyping;
using Helix.MiddleEnd.Interpreting;
using System.Collections.Immutable;

namespace Helix.MiddleEnd.TypeChecking {
    internal class PredicateStore {
        private ImmutableDictionary<IValueLocation, CnfTerm> predicates
            = ImmutableDictionary<IValueLocation, CnfTerm>.Empty;

        private readonly AnalysisContext context;

        private PredicateStore(
            AnalysisContext context,
            ImmutableDictionary<IValueLocation, CnfTerm> preds) {

            this.context = context;
            this.predicates = preds;
        }

        public CnfTerm this[IValueLocation index] {
            get => this.GetPredicate(index);
            set => this.SetPredicate(index, value);
        }

        public CnfTerm this[string name] => this.GetPredicate(name);

        public PredicateStore(AnalysisContext context) {
            this.context = context;
        }

        public PredicateStore CreateScope() {
            return new PredicateStore(this.context, this.predicates);
        }

        public bool WasModifiedBy(PredicateStore other) {
            var keys = this.predicates.Keys.Intersect(other.predicates.Keys);

            foreach (var key in keys) {
                if (this.predicates[key] != other.predicates[key]) {
                    return true;
                }
            }

            return false;
        }

        public PredicateStore MergeWith(PredicateStore other) {
            var resultValues = new Dictionary<IValueLocation, CnfTerm>();
            var keys = this.predicates.Keys.Union(other.predicates.Keys);

            foreach (var key in keys) {
                if (this.predicates.ContainsKey(key) && other.predicates.ContainsKey(key)) {
                    // TODO: Fix not having a location here
                    resultValues[key] = this.predicates[key].Or(other.predicates[key]);
                }
                else if (this.predicates.ContainsKey(key)) {
                    resultValues[key] = this.predicates[key];
                }
                else {
                    resultValues[key] = other.predicates[key];
                }
            }

            return new PredicateStore(this.context, resultValues.ToImmutableDictionary());
        }

        public void SetPredicate(IValueLocation location, CnfTerm predicate) {
            this.predicates = this.predicates.SetItem(location, predicate);
        }

        public void SetPredicate(IValueLocation location, IEnumerable<CnfTerm> predicates) {
            var pred = predicates.Aggregate((x, y) => x.Or(y));

            this.predicates = this.predicates.SetItem(location, pred);
        }

        public void ClearPredicate(IValueLocation location) {
            this.predicates = this.predicates.Remove(location);
        }

        public CnfTerm GetPredicate(IValueLocation location) {
            if (location is NamedLocation named) {
                return this.GetPredicate(named.Name);
            }


            Assert.IsTrue(this.predicates.ContainsKey(location));
            return this.predicates[location];
        }

        public CnfTerm GetPredicate(string name) {
            var loc = new NamedLocation(name);

            if (long.TryParse(name, out _) || name == "void") {
                return CnfTerm.Empty;
            }
            else if (bool.TryParse(name, out var b)) {
                return (CnfTerm)new BooleanLiteralPredicate(b);
            }
            else if (this.context.Types[loc] is SingularBoolType bb) {
                return (CnfTerm)new BooleanLiteralPredicate(bb.Value);
            }
            else if (this.predicates.TryGetValue(loc, out var cnf)) {
                return cnf;
            }
            else {
                return CnfTerm.Empty;
            }
        }

        public void MutateLocation(IValueLocation location, CnfTerm mutateWith) {
            foreach (var (key, value) in this.predicates) {
                if (key == location) {
                    this.predicates = this.predicates.SetItem(key, mutateWith);
                }
                else if (value.UsesVariable(location)) {
                    this.predicates = this.predicates.SetItem(key, CnfTerm.Empty);
                }
            }
        }
    }
}
