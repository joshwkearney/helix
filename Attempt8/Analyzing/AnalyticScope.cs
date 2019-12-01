using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt12.Analyzing {
    public class AnalyticScope {
        public ImmutableDictionary<string, VariableLocation> Variables { get; }

        public ImmutableDictionary<string, ISymbol> Types { get; }

        public ImmutableDictionary<ISymbol, ImmutableList<(string name, VariableLocation location)>> TypeMembers { get; }

        public AnalyticScope() {
            this.Variables = ImmutableDictionary<string, VariableLocation>.Empty;
            this.Types = ImmutableDictionary<string, ISymbol>.Empty;
            this.TypeMembers = ImmutableDictionary<ISymbol, ImmutableList<(string name, VariableLocation location)>>.Empty;
        }

        public AnalyticScope(
            ImmutableDictionary<string, VariableLocation> symbols, 
            ImmutableDictionary<string, ISymbol> types,
            ImmutableDictionary<ISymbol, ImmutableList<(string name, VariableLocation location)>> symbolExtensions
        ) {
            this.Variables = symbols;
            this.Types = types;
            this.TypeMembers = symbolExtensions;
        }

        public AnalyticScope AddVariable(string name, VariableLocation value) {
            return new AnalyticScope(this.Variables.Add(name, value), this.Types, this.TypeMembers);
        }

        public AnalyticScope AddType(string name, ISymbol value) {
            return new AnalyticScope(this.Variables, this.Types.Add(name, value), this.TypeMembers);
        }

        public AnalyticScope AddStaticMember(ISymbol type, string name, VariableLocation member) {
            var newTypeMembers = this.TypeMembers;

            if (!newTypeMembers.TryGetValue(type, out var members)) {
                newTypeMembers = newTypeMembers.Add(
                    type,
                    ImmutableList<(string name, VariableLocation location)>.Empty
                );
            }

            newTypeMembers = newTypeMembers.SetItem(type, newTypeMembers[type].Add((name, member)));

            return new AnalyticScope(this.Variables, this.Types, newTypeMembers);
        }

        public bool TryGetTypeMembers(ISymbol type, string name, out ImmutableList<VariableLocation> locations) {
            if (this.TypeMembers.TryGetValue(type, out var list)) {
                locations = list.Where(x => x.name == name).Select(x => x.location).ToImmutableList();
                return locations.Count > 0;
            }

            locations = null;
            return false;
        }
    }
}