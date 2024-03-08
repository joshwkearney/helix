using Helix.Common;
using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using System.Collections.Immutable;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeStore {
        private ImmutableDictionary<IValueLocation, IHelixType> values
            = ImmutableDictionary<IValueLocation, IHelixType>.Empty;

        private readonly AnalysisContext context;

        private TypeStore(
            AnalysisContext context,
            ImmutableDictionary<IValueLocation, IHelixType> currentTypes) {

            this.context = context;
            this.values = currentTypes;
        }

        public IHelixType this[IValueLocation index] {
            get => this.GetType(index);
            set => this.SetType(index, value);
        }

        public IHelixType this[string name] => this.GetType(name);

        public TypeStore(AnalysisContext context) {
            this.context = context;
        }

        public TypeStore CreateScope() {
            return new TypeStore(this.context, this.values);
        }

        public TypeStore MergeWith(TypeStore other) {
            var resultValues = new Dictionary<IValueLocation, IHelixType>();
            var keys = this.values.Keys.Union(other.values.Keys);

            foreach (var key in keys) {
                if (this.values.ContainsKey(key) && other.values.ContainsKey(key)) {
                    // TODO: Fix not having a location here
                    resultValues[key] = this.context.Unifier.UnifyWithConvert(this.values[key], other.values[key], default);
                }
                else if (this.values.ContainsKey(key)) {
                    resultValues[key] = this.values[key];
                }
                else {
                    resultValues[key] = other.values[key];
                }
            }

            return new TypeStore(this.context, resultValues.ToImmutableDictionary());
        }

        public void ClearType(IValueLocation location) {
            this.values = this.values.SetItem(location, this.values[location].GetSupertype());
        }

        private void SetType(IValueLocation location, IHelixType valueType) {
            this.values = this.values.SetItem(location, valueType);
        }

        private IHelixType GetType(IValueLocation location) {
            if (location is NamedLocation named) {
                if (this.GetSingularTypes(named.Name).TryGetValue(out var type)) {
                    return type;
                }
            }

            Assert.IsTrue(this.values.ContainsKey(location));
            return this.values[location];
        }

        private IHelixType GetType(string name) => this.GetType(new NamedLocation(name));

        private Option<IHelixType> GetSingularTypes(string name) {
            if (long.TryParse(name, out var longValue)) {
                return new SingularWordType(longValue);
            }
            else if (bool.TryParse(name, out var boolValue)) {
                return new SingularBoolType(boolValue);
            }
            else if (name == "void") {
                return new VoidType();
            }
            else {
                return Option.None;
            }
        }
    }
}
