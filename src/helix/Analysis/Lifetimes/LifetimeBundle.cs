using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis.Lifetimes {
    public class LifetimeBundle {
        public IReadOnlyDictionary<IdentifierPath, IReadOnlyList<Lifetime>> ComponentLifetimes { get; }

        public IReadOnlyList<Lifetime> ScalarLifetimes => this.ComponentLifetimes[new IdentifierPath()];

        public IEnumerable<Lifetime> AllLifetimes => this.ComponentLifetimes.SelectMany(x => x.Value);

        public LifetimeBundle(IReadOnlyDictionary<IdentifierPath, IReadOnlyList<Lifetime>> lifetimes) {
            this.ComponentLifetimes = lifetimes;
        }

        public LifetimeBundle() {
            this.ComponentLifetimes = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>() {
                { new IdentifierPath(), Array.Empty<Lifetime>() }
            };
        }

        public LifetimeBundle(IReadOnlyList<Lifetime> lifetimes) {
            this.ComponentLifetimes = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>() {
                { new IdentifierPath(), lifetimes }
            };
        }
    }
}